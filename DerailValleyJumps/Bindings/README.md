# Bindings

A framework mod for other mods to easily add new key/button bindings into the game.

## TODO

- support axis

## Setup

To add bindings to your mod you must:

1. wait for the game to be ready
2. call a method to add in your default bindings

```cs
public static class Main
{
    public static Settings settings;

    private static bool Load(UnityModManager.ModEntry modEntry)
    {
        // step 1
        BindingsHelper.OnReady += () =>
        {
            settings = Settings.Load<Settings>(modEntry);

            // step 2
            settings.AddDefaultBindings();

            harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}
```

Then define some actions and a method to your settings class to add default bindings:

```cs
public static class Actions
{
    public static int RearJatoActivate = 150;
    public static int FrontJatoActivate = 151;
}

public class Settings : UnityModManager.ModSettings
{
    public List<BindingInfo> Bindings;

    public void AddDefaultBindings()
    {
        var defaultBindings = new List<BindingInfo>()
        {
            new BindingInfo("Rear JATO", Actions.RearJatoActivate, KeyCode.LeftShift),
            new BindingInfo("Front JATO", Actions.FrontJatoActivate, KeyCode.LeftControl),
        };

        // necessary because UMM doesn't merge in Lists properly
        if (Bindings.Count == 0)
            Bindings = defaultBindings;

        // so the panel shows your bindings
        BindingsAPI.RegisterBindings(Main.ModEntry, Bindings);
    }
}
```

## Getting binding values

From an `Update()` loop get a binding value with:

```cs
if (BindingsAPI.GetIsPressed(Actions.RearJatoActivate))
{
    // BOOST!
}
```

or (slightly slower):

```cs
if (BindingsHelper.GetIsPressed(myBinding))
{
    // BOOST!
}
```

or (more slightly slower):

```cs
if (BindingsHelper.GetIsPressed(Main.settings.Bindings, Actions.RearJatoActivate))
{
    // BOOST!
}
```

## Outputting bindings

You can draw the editor in your mod settings:

```cs
public static class Main
{
    static void OnGUI(UnityModManager.ModEntry modEntry)
    {
        settings.Draw(modEntry);

        // add this
        BindingsHelperUI.DrawBindings(settings.Bindings);
    }
}
```

You can draw it inside any GUI:

```cs
public MyComponent : MonoBehavior
{
    void OnGUI()
    {
        void OnUpdate()
        {
            ModEntry.OnSaveGUI();
        }

        BindingsHelperUI.DrawBindings(settings.Bindings, OnUpdate);
    }
}
```

You can draw a single binding too:

```cs
public MyComponent : MonoBehavior
{
    void OnGUI()
    {
        void OnUpdate()
        {
            ModEntry.OnSaveGUI();
        }

        BindingsHelperUI.DrawBinding(myAwesomeBinding, OnUpdate);
    }
}
```
