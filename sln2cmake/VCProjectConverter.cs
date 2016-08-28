using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using EnvDTE;
using Microsoft.VisualStudio.VCProjectEngine;

namespace Sln2CMake
{
    class VCProjectConverter
    {
        public static Regex SourceExtention = new Regex(@"\.(c(pp)?)$");

        static private string Strip(string text, params string[] strips)
        {
            foreach (var strip in strips)
            {
                text = text.Replace(strip, "");
            }
            return text;
        }

        static private string Indent(int count)
        {
            return "".PadLeft(count, '\t');
        }

        static private void ParseIncludeDirectories(StreamWriter streamWriter, VCCLCompilerTool tool, int indent)
        {
            var indentText = Indent(indent);

            var items = Strip(tool.AdditionalIncludeDirectories, "%(AdditionalIncludeDirectories)", "$(NOINHERIT)").Replace('\\', '/').Split(';');
            streamWriter.WriteLine(indentText + "include_directories(");
            foreach (var item in items)
            {
                if (item == "")
                    continue;

                streamWriter.WriteLine(Indent(indent + 1) + "{0}", item);
            }
            streamWriter.WriteLine(indentText + ")");
            streamWriter.WriteLine();
        }

        static private void ParseDefines(StreamWriter streamWriter, VCCLCompilerTool tool, int indent)
        {
            var indentText = Indent(indent);

            var items = Strip(tool.PreprocessorDefinitions, "_WIN32", "WIN32", "_USRDLL", "_WINDOWS", "WINDOWS", "_LIB", "_MBCS", "_MSC_VER", "_DEBUG", "PROFILE", "NDEBUG", "DEBUG").Split(';');
            streamWriter.WriteLine(indentText + "add_definitions(");
            foreach (var item in items)
            {
                if (item == "")
                    continue;

                streamWriter.WriteLine(Indent(indent + 1) + "-D{0}", item);
            }
            streamWriter.WriteLine(indentText + ")");
            streamWriter.WriteLine();
        }

        static private void ParseSourceFile(StreamWriter streamWriter, IVCCollection files, Regex regex, int indent)
        {
            if (files.Count > 0)
            {
                for (int i = 1, n = files.Count; i <= n; ++i)
                {
                    try
                    {
                        var file = files.Item(i) as VCFile;
                        var path = file.RelativePath;
                        if (regex == null || regex.Match(path).Success)
                        {
                            streamWriter.WriteLine(Indent(indent) + "{0}", path.Replace("\\", "/"));
                        }
                    }
                    catch (System.Exception)
                    {
                        // maybe not a VCFile, that's why we dont use foreach, skip it
                    }
                }
            }
        }

        static private void ParseTarget(StreamWriter streamWriter, VCProject vcproject, VCConfiguration configuration, int indent)
        {
            var indentText = Indent(indent);

            switch (configuration.ConfigurationType)
            {
            case ConfigurationTypes.typeApplication:
                streamWriter.WriteLine(indentText + "add_executable({0}", vcproject.Name);
                ParseSourceFile(streamWriter, (IVCCollection)vcproject.Files, SourceExtention, indent + 1);
                streamWriter.WriteLine(indentText + ")");
                streamWriter.WriteLine();
                break;
            case ConfigurationTypes.typeDynamicLibrary:
                streamWriter.WriteLine(indentText + "set(CMAKE_LIBRARY_OUTPUT_DIRECTORY ${PROJECT_BINARY_DIR}/lib)");
                streamWriter.WriteLine(indentText + "set(ARCHIVE_OUTPUT_DIRECTORY ${PROJECT_BINARY_DIR}/lib)");
                streamWriter.WriteLine();
                streamWriter.WriteLine(indentText + "add_library({0}", vcproject.Name);
                ParseSourceFile(streamWriter, (IVCCollection)vcproject.Files, SourceExtention, indent + 1);
                streamWriter.WriteLine(indentText + ")");
                streamWriter.WriteLine();
                break;
            case ConfigurationTypes.typeStaticLibrary:
                streamWriter.WriteLine(indentText + "set(CMAKE_LIBRARY_OUTPUT_DIRECTORY ${PROJECT_BINARY_DIR}/lib)");
                streamWriter.WriteLine(indentText + "set(ARCHIVE_OUTPUT_DIRECTORY ${PROJECT_BINARY_DIR}/lib)");
                streamWriter.WriteLine();
                streamWriter.WriteLine(indentText, "add_library({0} STATIC", vcproject.Name);
                ParseSourceFile(streamWriter, (IVCCollection)vcproject.Files, SourceExtention, indent + 1);
                streamWriter.WriteLine(indentText + ")");
                streamWriter.WriteLine();
                break;
            }
        }

        static private void ParseLibraryDirectories(StreamWriter streamWriter, VCLinkerTool tool, int indent)
        {
            var indentText = Indent(indent);

            var items = tool.AdditionalLibraryDirectories.Split(';');
            streamWriter.WriteLine(indentText + "link_directories(");
            foreach (var item in items)
            {
                streamWriter.WriteLine(Indent(indent + 1) + "{0}", item);
            }
            streamWriter.WriteLine(indentText + ")");
            streamWriter.WriteLine();
        }

        static private void ParseLibraries(StreamWriter streamWriter, VCProject project, VCLinkerTool tool, int indent)
        {
            var indentText = Indent(indent);

            var dependencies = tool.AdditionalDependencies;
            if (dependencies != "")
            {
                var items = dependencies.Split(' ');
                streamWriter.WriteLine(indentText + "target_link_libraries({0}", project.Name);
                foreach (var item in items)
                {
                    streamWriter.WriteLine(Indent(indent + 1) + "{0}", item.Replace(".lib", ""));
                }
                streamWriter.WriteLine(indentText + ")");
                streamWriter.WriteLine();
            }
        }

        static private void ParseFilters(StreamWriter streamWriter, VCProject project, Regex regex, int indent)
        {
            var indentText = Indent(indent);

            foreach (VCFilter filter in (IVCCollection)project.Filters)
            {
                streamWriter.WriteLine(indentText + "source_group(\"{0}\" FILES", filter.CanonicalName.Replace("\\", "\\\\"));

                ParseSourceFile(streamWriter, (IVCCollection)filter.Files, regex, indent + 1);

                streamWriter.WriteLine(indentText + ")");
                streamWriter.WriteLine();
            }
        }

        public static void Run(IServiceProvider serviceProvider, Project project)
        {
            var vcproject = project.Object as VCProject;

            // TODO:
            var cmakeListsFile = vcproject.ProjectDirectory + "CMakeLists.txt";

            Debug.OutputBuildMessage(serviceProvider, string.Format("Convert: {0} to {1}", vcproject.ProjectFile, cmakeListsFile));

            using (var streamWriter = new StreamWriter(cmakeListsFile, false, Encoding.Default))
            {
                streamWriter.WriteLine("# configurations");

                int indent = 0;

                foreach (VCConfiguration configuration in (IVCCollection)vcproject.Configurations)
                {
                    var configurationName = configuration.ConfigurationName;

                    streamWriter.WriteLine("if({0})", configurationName);

                    var tools = configuration.Tools as IVCCollection;

                    var compilerTool = tools.Item("VCCLCompilerTool") as VCCLCompilerTool;
                    if (compilerTool != null)
                    {
                        ParseIncludeDirectories(streamWriter, compilerTool, indent + 1);
                        ParseDefines(streamWriter, compilerTool, indent + 1);
                    }

                    ParseTarget(streamWriter, vcproject, configuration, indent + 1);

                    var linkerTool = tools.Item("VCLinkerTool") as VCLinkerTool;
                    if (linkerTool != null)
                    {
                        ParseLibraryDirectories(streamWriter, linkerTool, indent + 1);
                        ParseLibraries(streamWriter, vcproject, linkerTool, indent + 1);
                    }

                    streamWriter.WriteLine("endif()");
                    streamWriter.WriteLine();
                }

                ParseFilters(streamWriter, vcproject, null, indent);
            }
        }
    }
}
