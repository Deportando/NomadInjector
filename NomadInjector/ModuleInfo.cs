using System;
using System.ComponentModel;
using System.IO;

public class ModuleInfo
{
    public string ModuleName { get; set; }
    public string BaseAddress { get; set; } 
    public int Size { get; set; }
    public string FullPath { get; set; }

    public ModuleInfo(string name, IntPtr address, int size, string path)
    {
        ModuleName = name;
        BaseAddress = "0x" + address.ToInt64().ToString("X");
        Size = size;
        FullPath = path;
    }
}