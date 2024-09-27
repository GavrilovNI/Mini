using Mini.Attributes;
using Sandbox;
using System.Collections.Generic;

namespace Mini.UI.Menus;

public class MenuController : Component
{
    [Property]
    public Component? EscapeMenu { get; set; }
    [Property, HideIfNotNull(nameof(EscapeMenu))]
    public bool DisableDefaultEscapeMenu { get; set; } = false;

    private readonly List<Component> _menus = new();


    public void OpenMenu(Component menu, bool disablePreviuos = true)
    {
        if(!menu.IsValid())
            return;

        if(_menus.Count > 0 && disablePreviuos)
            _menus[^1].Enabled = false;

        menu.Enabled = true;
        _menus.Add(menu);
    }

    public void CloseMenu()
    {
        if(_menus.Count == 0)
            return;

        Component menu;
        do
        {
            menu = _menus[^1];
            _menus[^1].Enabled = false;
            _menus.RemoveAt(_menus.Count - 1);
        }
        while(!menu.IsValid() && _menus.Count > 0);

        if(_menus.Count > 0)
            _menus[^1].Enabled = true;
    }

    protected override void OnUpdate()
    {
        if(Input.EscapePressed)
        {
            if(_menus.Count > 0)
            {
                Input.EscapePressed = false;
                CloseMenu();
            }
            else if(EscapeMenu.IsValid())
            {
                Input.EscapePressed = false;
                OpenMenu(EscapeMenu);
            }
            else if(DisableDefaultEscapeMenu)
            {
                Input.EscapePressed = false;
            }
        }
    }
}
