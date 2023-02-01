﻿using System;
using System.IO;
using System.IO.FileSystem.AccessControl;
using System.Security.AccessControl;
using System.Threading.Tasks;
using System.Threading;
using ElastiBuild.Extensions;
using Elastic.Installer;
using SimpleExec;


namespace ElastiBuild.BullseyeTargets
{
    public class SignMsiPackageTarget : SignToolTargetBase<SignMsiPackageTarget>
    {
        public static async Task RunAsync(BuildContext ctx)
        {
            var ap = ctx.GetArtifactPackage();

            string filePath = Path.Combine(
                 ctx.OutDir,
                 ap.CanonicalTargetName,
                 Path.GetFileNameWithoutExtension(ap.FileName) + MagicStrings.Ext.DotMsi
            );

            FileSecurity security = File.GetAccessControl(filePath);
            security.SetAccessRuleProtection(false, false);

            FileSystemAccessRule rule = new FileSystemAccessRule(
                "Users",
                FileSystemRights.FullControl,
                InheritanceFlags.None,
                PropagationFlags.None,
                AccessControlType.Allow
            );
            security.SetAccessRule(rule);
            File.SetAccessControl(filePath, security);
            Console.WriteLine("Access control set on " + filePath);

            var SignToolExePath = Path.Combine(
                ctx.ToolsDir,
                MagicStrings.Dirs.Cert,
                MagicStrings.Files.SignToolExe);

            bool signed = false;
            int tryCount = ctx.Config.TimestampUrls.Count;

            for (int tryNr = 0; tryNr < tryCount; ++tryNr)
            {
                var timestampUrl = ctx.Config.TimestampUrls[tryNr];
                var (certPass, SignToolArgs) = MakeSignToolArgs(ctx, timestampUrl);

                SignToolArgs += filePath.Quote();

                Console.WriteLine(SignToolExePath + " ");
                Console.WriteLine(SignToolArgs.Replace(certPass, "[redacted]"));

                try
                {
                    await Command.RunAsync(SignToolExePath, SignToolArgs, noEcho: true);
                    signed = true;
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"Error: SigTool failed, check it's output: {ex.Message}\n" +
                        $"{tryCount - tryNr - 1} server(s) left to try.");
                }
            }

            if (!signed)
                throw new Exception("Error: Failed to sign msi after all retries.");
        }
    }
}
