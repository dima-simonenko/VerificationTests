using System.Collections.Generic;

namespace Report.DataStructures
{
    public struct TestGroupConfig
    {
        public double F { get; set; }
        public double Width { get; set; }
        public double Length { get; set; }
        public double V0 { get; set; }
        public double A { get; set; }
        public double D0 { get; set; }
        public Config StairsUP { get; set; }
        public Config StairsDown { get; set; }
        public Config RampUP { get; set; }
        public Config RampDown { get; set; }
    }

    public struct TestConfig
    {
        public double Density { get; set; }
        public double? Width { get; set; }
    }

    public struct TestInfo
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public Config TestConfig { get; set; }
    }

    public struct Config
    {
        public double F { get; set; }
        public double Width { get; set; }
        public double Length { get; set; }
        public double V0 { get; set; }
        public double A { get; set; }
        public double D0 { get; set; }
        public double Density { get; set; }
        public string Description { get; set; }
    }

    public struct TestGroup
    {
        public List<TestInfo> Tests { get; set; }
        public TestGroupConfig GroupConfig { get; set; }
        public string Description { get; set; }
    }
}