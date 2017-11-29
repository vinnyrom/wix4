// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Native
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;

    internal class WixNativeExe
    {
        private const int FiveMinutesInMilliseconds = 300000;
        private static readonly string PathToWixNativeExe;

        private readonly string commandLine;
        private readonly List<string> stdinLines = new List<string>();

        static WixNativeExe()
        {
            PathToWixNativeExe = Path.Combine(Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath), "x86\\wixnative.exe");
        }

        public WixNativeExe(params object[] args)
        {
            this.commandLine = String.Join(" ", QuoteArgumentsAsNecesary(args));
        }

        public void AddStdinLine(string line)
        {
            this.stdinLines.Add(line);
        }

        public void AddStdinLines(IEnumerable<string> lines)
        {
            this.stdinLines.AddRange(lines);
        }

        public IEnumerable<string> Run()
        {
            var wixNativeInfo = new ProcessStartInfo(PathToWixNativeExe, this.commandLine)
            {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                ErrorDialog = false,
                UseShellExecute = false
            };

            var stdoutLines = new List<string>();

            using (var process = Process.Start(wixNativeInfo))
            {
                process.OutputDataReceived += (s, a) => stdoutLines.Add(a.Data);
                process.BeginOutputReadLine();

                if (this.stdinLines.Count > 0)
                {
                    foreach (var line in this.stdinLines)
                    {
                        process.StandardInput.WriteLine(line);
                    }

                    // Trailing blank line indicates stdin complete.
                    process.StandardInput.WriteLine();
                }

                if (process.WaitForExit(FiveMinutesInMilliseconds))
                {
                    // If the process successfully exits documentation says we need to wait again
                    // without a timeout to ensure that all of the redirected output is captured.
                    //
                    process.WaitForExit();
                }

                if (process.ExitCode != 0)
                {
                    throw new Win32Exception(process.ExitCode);
                }
            }

            return stdoutLines;
        }

        private static IEnumerable<string> QuoteArgumentsAsNecesary(object[] args)
        {
            foreach (var arg in args)
            {
                if (arg is string str)
                {
                    if (String.IsNullOrEmpty(str))
                    {
                    }
                    else if (str.Contains(" ") && !str.StartsWith("\""))
                    {
                        yield return $"\"{str}\"";
                    }
                    else
                    {
                        yield return str;
                    }
                }
                else if (arg is int i)
                {
                    yield return i.ToString();
                }
                else
                {
                    throw new ArgumentException(nameof(args));
                }
            }
        }
    }
}
