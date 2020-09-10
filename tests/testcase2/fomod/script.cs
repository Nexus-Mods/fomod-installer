using System;

class Script : BaseScript
{
    public bool OnActivate()
    {
        GenerateDataFile("dummy.esp", new byte[] { 0x61, 0x62, 0x63 });
        return true;
    }
}