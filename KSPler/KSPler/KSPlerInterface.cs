using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

using fuel = KSPler.FuelCalculations;

class KSPlerInterface : PartModule
{
    enum DisplayMode { EDITORMODE, FLYINGMODE, NONE }
    DisplayMode current;
    List<Callback> callbacks;
    Rect area;

    void switchMode(DisplayMode d) { 
        current = d;
        foreach (Callback i in callbacks) {
            RenderingManager.RemoveFromPostDrawQueue(3, i);
        }
        if (d == DisplayMode.EDITORMODE) { 
            area = new Rect((Screen.width/3) - 40, (Screen.height/3) - 40, 200, 200);
            var c = new Callback(drawVehicleStats);
            callbacks.Add(c);
            RenderingManager.AddToPostDrawQueue(3, c);
        }

        if (d == DisplayMode.FLYINGMODE) {
            Print("GUI Swithed to flight");
            area = new Rect((Screen.width / 3) - 40, (Screen.height / 3) - 40, 200, 200);
            var c = new Callback(drawOrbitStats);
            callbacks.Add(c);
            RenderingManager.AddToPostDrawQueue(3, c);
        }
    }

    void drawOrbitStats() {
        GUI.skin = HighLogic.Skin;
        var w = GUILayout.Window(1, area, flightDVGUI, "Where Is To Be Goings", GUILayout.MinWidth(200));
    }

    void drawVehicleStats() {
        GUI.skin = HighLogic.Skin;
        var w = GUILayout.Window(1, area, vehicleGUI, "How Big To Be Makings", GUILayout.MinWidth(200));
    }

    void flightDVGUI(int windowID) {
        Print("Flight DVGUI");
        GUIStyle sty = new GUIStyle(GUI.skin.window);
        sty.normal.textColor = sty.focused.textColor = Color.white;
        sty.hover.textColor = sty.active.textColor = Color.gray;
        sty.onNormal.textColor = sty.onFocused.textColor = sty.onActive.textColor = sty.onHover.textColor = Color.green;
        sty.padding = new RectOffset(12, 12, 12, 12);

        var lst = traverseChildren(this.vessel.rootPart);
        Print("lst computed");

        GUILayout.BeginVertical();
        {
            GUILayout.TextArea(String.Format("Apoapsis {0:N3}m",
                this.vessel.orbit.ApR));
            GUILayout.TextArea(String.Format("Periapsis; {0:N3}m",
                this.vessel.orbit.PeR));
        }
        GUILayout.EndVertical();

        GUILayout.BeginVertical();
        {
            GUILayout.TextArea(String.Format("Surface weight: {0:N3} kN",
                fuel.getCurrentSurfaceWeight(lst)));
            GUILayout.TextArea(String.Format("First stage thrust: {0:N3} kN",
                fuel.getInitialMaxThrust(lst)));
            GUILayout.TextArea(String.Format("Suface Thrust-weight ratio: {0:N3}",
                fuel.getInitialMaxThrust(lst) / fuel.getCurrentSurfaceWeight(lst)));
            GUILayout.TextArea(String.Format("Delta-V (Air, Vac): {0:N3}, {1:N3}",
                fuel.deltaVAir(lst), fuel.deltaVVac(lst)));
        }        
        GUILayout.EndVertical();

        GUI.DragWindow(new Rect(0, 0, 10000, 20));
    }

    IEnumerable<Part> traverseChildren(Part p) {
        var desc = (from c in p.children select traverseChildren(c)).Aggregate((IEnumerable<Part>)(new LinkedList<Part>()), (a, b) => a.Concat(b));
        return desc.Concat(new[] { p });
    }

    void vehicleGUI(int windowID) {
        GUIStyle sty = new GUIStyle(GUI.skin.window);
        sty.normal.textColor = sty.focused.textColor = Color.white;
        sty.hover.textColor = sty.active.textColor = Color.gray;
        sty.onNormal.textColor = sty.onFocused.textColor = sty.onActive.textColor = sty.onHover.textColor = Color.green;
        sty.padding = new RectOffset(12, 12, 12, 12);

        var lst = EditorLogic.SortedShipList;

        GUILayout.BeginVertical();
        {
            GUILayout.TextArea(String.Format("Surface weight: {0:N3} kN",
                fuel.getCurrentSurfaceWeight(lst)));
            GUILayout.TextArea(String.Format("First stage thrust: {0:N3} kN",
                fuel.getInitialMaxThrust(lst)));
            GUILayout.TextArea(String.Format("Suface Thrust-weight ratio: {0:N3}",
                fuel.getInitialMaxThrust(lst) / fuel.getCurrentSurfaceWeight(lst)));
            GUILayout.TextArea(String.Format("Delta-V (Air, Vac): {0:N3}, {1:N3}",
                fuel.deltaVAir(lst), fuel.deltaVVac(lst)));
        }
        GUILayout.EndVertical();

        GUI.DragWindow(new Rect(0, 0, 10000, 20));
    }

    void Print(string message) {
        print("[KSPler] " + message);
    }

    public override void OnStart(PartModule.StartState state) {
        base.OnStart(state);
        callbacks = new List<Callback>();
        if (state == StartState.Editor) {
            Print("Entering editor mode");
            switchMode(DisplayMode.EDITORMODE);
        } else if (state == StartState.Flying || state == StartState.PreLaunch
            || state == StartState.Orbital || state == StartState.SubOrbital ||
            state == StartState.Docked || state == StartState.Landed || state == StartState.Splashed || state == StartState.None) {
            Print("Entering flight mode");
            switchMode(DisplayMode.FLYINGMODE);
        } else {
            Print("Doing nothing. Not really, because I'm a fucking asshole hahahahahahahahahaha");
            switchMode(DisplayMode.FLYINGMODE);
        }
    }

    public override void OnInactive() {
        base.OnInactive();
        Print("Closing down.");
        switchMode(DisplayMode.NONE);
    }

}