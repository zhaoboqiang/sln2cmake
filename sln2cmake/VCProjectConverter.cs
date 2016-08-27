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

        static private VCConfiguration GetConfiguration(VCProject project, string platformName)
        {
            foreach (VCConfiguration configuration in (IVCCollection)project.Configurations)
            {
                var ConfigPlatform = configuration.Platform as VCPlatform;
                if (ConfigPlatform.Name.Equals(platformName))
                {
                    return configuration;
                }
            }

            return null;
        }
        static private void ParseIncludeDirectories(StreamWriter streamWriter, VCCLCompilerTool tool)
        {
            var items = Strip(tool.AdditionalIncludeDirectories, "%(AdditionalIncludeDirectories)", "$(NOINHERIT)").Replace('\\', '/').Split(';');
            streamWriter.WriteLine("include_directories(");
            foreach (var item in items)
            {
                if (item == "")
                    continue;

                streamWriter.WriteLine("\t{0}", item);
            }
            streamWriter.WriteLine(")");
            streamWriter.WriteLine();
        }

        static private void ParseDefines(StreamWriter streamWriter, VCCLCompilerTool tool)
        {
            var items = Strip(tool.PreprocessorDefinitions, "_WIN32", "WIN32", "_USRDLL", "_WINDOWS", "WINDOWS", "_LIB", "_MBCS", "_MSC_VER", "_DEBUG", "PROFILE", "NDEBUG", "DEBUG").Split(';');
            streamWriter.WriteLine("add_definitions(");
            foreach (var item in items)
            {
                if (item == "")
                    continue;

                streamWriter.WriteLine("\t-D{0}", item);
            }
            streamWriter.WriteLine(")");
            streamWriter.WriteLine();
        }

        static private void ParseSourceFile(StreamWriter streamWriter, IVCCollection files, Regex regex)
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
                            streamWriter.WriteLine("\t{0}", path.Replace("\\", "/"));
                        }
                    }
                    catch (System.Exception)
                    {
                        // maybe not a VCFile, that's why we dont use foreach, skip it
                    }
                }
            }
        }

        static private void ParseTarget(StreamWriter streamWriter, VCProject vcproject, VCConfiguration configuration)
        {
            switch (configuration.ConfigurationType)
            {
            case ConfigurationTypes.typeApplication:
                streamWriter.WriteLine("add_executable({0}", vcproject.Name);
                ParseSourceFile(streamWriter, (IVCCollection)vcproject.Files, SourceExtention);
                streamWriter.WriteLine(")");
                streamWriter.WriteLine();
                break;
            case ConfigurationTypes.typeDynamicLibrary:
                streamWriter.WriteLine("set(CMAKE_LIBRARY_OUTPUT_DIRECTORY ${PROJECT_BINARY_DIR}/lib)");
                streamWriter.WriteLine("set(ARCHIVE_OUTPUT_DIRECTORY ${PROJECT_BINARY_DIR}/lib)");
                streamWriter.WriteLine();
                streamWriter.WriteLine("add_library({0}", vcproject.Name);
                ParseSourceFile(streamWriter, (IVCCollection)vcproject.Files, SourceExtention);
                streamWriter.WriteLine(")");
                streamWriter.WriteLine();
                break;
            case ConfigurationTypes.typeStaticLibrary:
                streamWriter.WriteLine("set(CMAKE_LIBRARY_OUTPUT_DIRECTORY ${PROJECT_BINARY_DIR}/lib)");
                streamWriter.WriteLine("set(ARCHIVE_OUTPUT_DIRECTORY ${PROJECT_BINARY_DIR}/lib)");
                streamWriter.WriteLine();
                streamWriter.WriteLine("add_library({0} STATIC", vcproject.Name);
                ParseSourceFile(streamWriter, (IVCCollection)vcproject.Files, SourceExtention);
                streamWriter.WriteLine(")");
                streamWriter.WriteLine();
                break;
            }
        }

        static private void ParseLibraryDirectories(StreamWriter streamWriter, VCLinkerTool tool)
        {
            var items = tool.AdditionalLibraryDirectories.Split(';');
            streamWriter.WriteLine("link_directories(");
            foreach (var item in items)
            {
                streamWriter.WriteLine("\t{0}", item);
            }
            streamWriter.WriteLine(")");
            streamWriter.WriteLine();
        }

        static private void ParseLibraries(StreamWriter streamWriter, VCProject project, VCLinkerTool tool)
        {
            var dependencies = tool.AdditionalDependencies;
            if (dependencies != "")
            {
                var items = dependencies.Split(' ');
                streamWriter.WriteLine("target_link_libraries({0}", project.Name);
                foreach (var item in items)
                {
                    streamWriter.WriteLine("\t{0}", item.Replace(".lib", ""));
                }
                streamWriter.WriteLine(")");
                streamWriter.WriteLine();
            }
        }

        static private void ParseFilters(StreamWriter streamWriter, VCProject project, Regex regex)
        {
            foreach (VCFilter filter in (IVCCollection)project.Filters)
            {
                streamWriter.WriteLine("source_group(\"{0}\" FILES", filter.CanonicalName.Replace("\\", "\\\\"));

                ParseSourceFile(streamWriter, (IVCCollection)filter.Files, regex);

                streamWriter.WriteLine(")");
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
                var configuration = GetConfiguration(vcproject, "x64");
                if (configuration == null)
                    configuration = GetConfiguration(vcproject, "Win32");
                if (configuration == null)
                    configuration = GetConfiguration(vcproject, "Arm");
                if (configuration != null)
                {
                    var tools = configuration.Tools as IVCCollection;

                    var compilerTool = tools.Item("VCCLCompilerTool") as VCCLCompilerTool;
                    if (compilerTool != null)
                    {
                        ParseIncludeDirectories(streamWriter, compilerTool);
                        ParseDefines(streamWriter, compilerTool);
                    }

                    ParseTarget(streamWriter, vcproject, configuration);

                    var linkerTool = tools.Item("VCLinkerTool") as VCLinkerTool;
                    if (linkerTool != null)
                    {
                        // ParseLibraryDirectories(streamWriter, linkerTool);
                        ParseLibraries(streamWriter, vcproject, linkerTool);
                    }
                }

                ParseFilters(streamWriter, vcproject, null);
            }
        }
    }
}
