# SAVis

This program visualises custom sorting algorithms that can be loaded at runtime.

## Interface

You interact with the visualiser through the built in command line (opened with the `~` keyboard key).
The full list of commands can be accessed through the `cmd` command.

## Custom sorters

The custom sorting scripts are written in `C#` and have to be placed as individual source files in the `/scripts` directory created when you first run the program.
The `load` command loads all scripts placed in the directory.

## Scripts

A valid script contains at least one `public class` that implements the `ISorter` interface from the `SAVis.API` namespace.
The whole sorting action occurs in the `Update` method, you access the element array through the `SortingContext` object and return a `bool` value that indicates whether the array has been sorted (`true` or `false`).

## Example

```

// this is the default sorter that is always available in the program
public class DefaultSorter : ISorter
{
    public string Name => "Default sorter";
    public string Author => "Adam Papieros";

    uint _current = 0;
    public IEnumerator<bool> Update(SortingContext ctx)
    {
        bool unsorted = true;
        while (unsorted)
        {
            unsorted = false;
            for (uint i = 0; i < ctx.ArraySize - 1; i++) 
            {
                var a = ctx.GetValueAt(i);
                var b = ctx.GetValueAt(i + 1);
                if (a > b)
                {
                    ctx.SwapValues(i, i + 1);
                    unsorted = true;
                }
                yield return false;
            }
        }
        yield return true;
    }
}

```