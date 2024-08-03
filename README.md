# SAVis

This program visualises custom sorting algorithms that can be loaded at runtime.

![image](https://github.com/user-attachments/assets/bb0d273b-32b0-4d36-ba97-0a936ce4cd1a)

![image](https://github.com/user-attachments/assets/be261a11-97d7-4103-bf1f-badf6f72366a)

## Interface

You interact with the visualiser through the built in command line (opened with the `~` keyboard key).
The full list of commands can be accessed through the `cmd` command.

## Custom sorters

The custom sorting scripts are written in `C#` and have to be placed as individual source files in the `/scripts` directory created when you first run the program.
The `load` command loads all scripts placed in the directory.

## Scripts

A valid script contains at least one `public class` that implements the `ISorter` interface from the `SAVis.API` namespace. This script is then compiled at program runtime and an object of this class is instantiated.

The whole sorting action occurs in the `Update` method, you access the element array through the `SortingContext` object and return a `bool` value that indicates whether the array has been sorted (`true` or `false`). To enable the program to visualise each step of the sorting process the script is expected to `yield` after every call to either `SortingContext.GetValueAt()` or `SortingContext.SwapValues()`. Thanks to this the execution of the script can be suspended so that all the values currently in use by the algorithm can be properly drawn (red bars for `GetValueAt()` and green for `SwapValues()` otherwise the whole sorting process would happen in one `Update()` call and the array would be sorted 'immediately'.

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
                yiled return false;
                var b = ctx.GetValueAt(i + 1);
                yield return false;
                if (a > b)
                {
                    ctx.SwapValues(i, i + 1);
                    yield return false;
                    unsorted = true;
                }
                yield return false;
            }
        }
        yield return true;
    }
}

```
