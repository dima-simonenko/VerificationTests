using System.Xml.Serialization;


public class TestGroup
{
    [XmlElement("Id")]
    public string Id { get; set; }
    [XmlElement("RegistratorEvacuationTimeMaxDiff")]
    public double RegistratorEvacuationTimeMaxDiff { get; set; }
    [XmlElement("RegistratorEvacuatedPeopleCountMaxDiff")]
    public double RegistratorEvacuatedPeopleCountMaxDiff { get; set; }
    [XmlElement("ScenarioEvacuationTimeMaxDiff")]
    public string ScenarioEvacuationTimeMaxDiff { get; set; }
    [XmlElement("ScenarioEvacuatedPeopleCountMaxDiff")]
    public double ScenarioEvacuatedPeopleCountMaxDiff { get; set; }
    [XmlElement("ScenarioConcourseTimeMaxDiff")]
    public string ScenarioConcourseTimeMaxDiff { get; set; }
    [XmlElement("ConfigLocation")]
    public string ConfigLocation { get; set; }
    [XmlElement("Description")]
    public string Description { get; set; }
    [XmlElement("Tests")]
    public Tests Tests { get; set; }
}
[XmlRoot("Tests")]
public class Tests
{
    [XmlElement("Test")]
    public Test[] TestList { get; set; }
}

[XmlRoot("Test")]
public class Test
{
    [XmlElement("Id")]
    public string Id { get; set; }
    [XmlElement("ProjectLocation")]
    public string ProjectLocation { get; set; }
    [XmlElement("ScenarioName")]
    public string ScenarioName { get; set; }
    [XmlElement("RegistratorName")]
    public string RegistratorName { get; set; }
    [XmlElement("RegistratorEvacuationTime")]
    public double RegistratorEvacuationTime { get; set; }
    [XmlElement("RegistratorEvacuatedPeopleCount")]
    public int RegistratorEvacuatedPeopleCount { get; set; }
    [XmlElement("ScenarioEvacuationTime")]
    public double ScenarioEvacuationTime { get; set; }
    [XmlElement("ScenarioEvacuatedPeopleCount")]
    public int ScenarioEvacuatedPeopleCount { get; set; }
    [XmlElement("ScenarioConcourseTime")]
    public double ScenarioConcourseTime { get; set; }
    [XmlElement("ConfigLocation")]
    public string ConfigLocation { get; set; }
    [XmlElement("Description")]
    public string Description { get; set; }
}