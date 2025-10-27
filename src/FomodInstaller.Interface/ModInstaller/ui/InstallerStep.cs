namespace FomodInstaller.Interface.ui;

public class InstallerStep
{
    public int id { get; set; }
    public string name { get; set; }
    public bool visible { get; set; }
    public GroupList optionalFileGroups { get; set; } = new();

    public InstallerStep(int id, string name, bool visible)
    {
        this.id = id;
        this.name = name;
        this.visible = visible;
    }
}