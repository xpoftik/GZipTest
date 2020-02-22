using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GZipTest.Arch
{
    public enum ArchResultStatus { 
        Success,
        Interrupted
    }

    public sealed class ArchResult
    {
        private ArchResult(string target, string archFilename, ArchResultStatus status)
        {
            if (!File.Exists(target)) {
                throw new ArgumentException("Parameter has to refer to an existing file.", nameof(target));
            }
            if (!String.IsNullOrEmpty(archFilename) && !File.Exists(archFilename)) {
                throw new ArgumentException("Parameter has to refer to an existing file.", nameof(archFilename));
            }

            TargetFileInfo = new FileInfo(target);
            if (!String.IsNullOrWhiteSpace(archFilename)) {
                ArchFileInfo = new FileInfo(archFilename);
            }
            Status = status;
        }

        public static ArchResult Success(string target, string archFilename) {
            if (String.IsNullOrWhiteSpace(target)) {
                throw new ArgumentNullException(nameof(target));
            }
            if (String.IsNullOrWhiteSpace(archFilename)) {
                throw new ArgumentNullException(nameof(archFilename));
            }

            return new ArchResult(target, archFilename, status: ArchResultStatus.Success);
        }

        public static ArchResult Interrupted(string target) {
            if (String.IsNullOrWhiteSpace(target)) {
                throw new ArgumentNullException(nameof(target));
            }

            return new ArchResult(target, archFilename: String.Empty, status: ArchResultStatus.Interrupted) ;
        }

        public FileInfo TargetFileInfo { get; }
        public FileInfo  ArchFileInfo { get; }

        public ArchResultStatus Status { get; }
        public bool IsSuccess { get => Status == ArchResultStatus.Success; }
        public bool IsInterrupted { get => Status == ArchResultStatus.Interrupted; }
    }
}
