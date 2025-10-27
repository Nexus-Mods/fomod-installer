namespace FomodInstaller.Interface.ui;

public class Option
{
    public int id { get; set; }
    public bool selected { get; set; }
    public bool preset { get; set; }
    public string name { get; set; }
    public string description { get; set; }
    public string image { get; set; }
    public string type { get; set; }
    public string conditionMsg { get; set; }

    public Option(int id, string name, string description, string image, bool selected, bool preset, string type, string conditionMsg)
    {
        this.id = id;
        this.name = name;
        this.description = description;
        this.image = image;
        this.selected = selected;
        this.preset = preset;
        this.type = type;
        this.conditionMsg = conditionMsg;
    }
}