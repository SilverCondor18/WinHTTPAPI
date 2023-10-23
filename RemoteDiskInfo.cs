using System;
using System.Collections.Generic;
using System.IO;

namespace WinHTTPAPI
{
    public class RemoteDiskInfo
    {
        public string DiskLabel { get; set; }
        public string DiskName { get; set; }
        public long FreeSpace { get; set; }
        public long TotalSize { get; set; }
        public DriveType DriveType { get; set; }
        public bool Ready { get; set; }
        public static RemoteDiskInfo FromDriveInfo(DriveInfo di)
        {
            return new RemoteDiskInfo()
            {
                DiskLabel = di.VolumeLabel,
                DiskName = di.Name,
                FreeSpace = di.AvailableFreeSpace,
                TotalSize = di.TotalSize,
                DriveType = di.DriveType,
                Ready = di.IsReady
            };
        }
        public static List<RemoteDiskInfo> FromList(DriveInfo[] drives)
        {
            List<RemoteDiskInfo> drivesInfo = new List<RemoteDiskInfo>();
            foreach (var drive in drives)
            {
                drivesInfo.Add(FromDriveInfo(drive));
            }
            return drivesInfo;
        }
        public static List<RemoteDiskInfo> FromList(List<DriveInfo> drives)
        {
            return FromList(drives.ToArray());
        }
        public static List<RemoteDiskInfo> FromLocalDrives()
        {
            return FromList(DriveInfo.GetDrives());
        }
    }
}
