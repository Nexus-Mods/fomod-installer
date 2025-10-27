namespace FomodInstaller.Interface.ui;

public class Group
{
    public int id { get; set; }
    public string name { get; set; }
    public string type { get; set; }
    public Option[] options { get; set; }

    public Group(int id, string name, string type, Option[] options)
    {
        this.id = id;
        this.name = name;
        this.type = type;
        this.options = options;
    }
}