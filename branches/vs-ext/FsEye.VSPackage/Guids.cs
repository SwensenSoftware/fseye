// Guids.cs
// MUST match guids.h
using System;

namespace StephenSwensen.FsEye_VSPackage
{
    static class GuidList
    {
        public const string guidFsEye_VSPackagePkgString = "c4fcc2d5-08f8-4e73-a218-14e5fd3a684d";
        public const string guidFsEye_VSPackageCmdSetString = "75b5d0c4-c410-4264-97e5-d2d8e1054744";
        public const string guidToolWindowPersistanceString = "90499915-0189-4cd9-ac5c-2de5c8277b7d";

        public static readonly Guid guidFsEye_VSPackageCmdSet = new Guid(guidFsEye_VSPackageCmdSetString);
    };
}