using System.Collections.Generic;
using UnityEngine;

namespace PTB.Networking.Menus.Interfaces
{
    public interface IMenu
    {
        void OpenMenu();
        void CloseMenu();
        void ResetMenu();
        void Refresh();

    }
}