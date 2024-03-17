using libNOM.cli.Args;

namespace libNOM.cli;


[ArgExceptionBehavior(ArgExceptionPolicy.StandardExceptionHandling)]
public class Executor
{
    [HelpHook, ArgShortcut("-?"), ArgDescription("Shows this help")]
    public bool Help { get; set; }

    [ArgActionMethod, ArgDescription("Adds the two operands")]
    public void Analyze(AnalyzeArgs args)
    {
        Console.WriteLine("Analyze");
    }

    [ArgActionMethod, ArgDescription("Adds the two operands")]
    public void Convert(ConvertArgs args)
    {
        Console.WriteLine("Convert");
    }

    [ArgActionMethod, ArgDescription("Divides the two operands")]
    public void Backup(FileOperationOneOperandArgs args)
    {
        Console.WriteLine("Backup");
    }

    [ArgActionMethod, ArgDescription("Divides the two operands")]
    public void Restore(FileOperationOneOperandArgs args)
    {
        Console.WriteLine("Restore");
    }

    [ArgActionMethod, ArgDescription("Copies the specified saves."), ArgExample("-s 1 2 -d 3 4", "Desc1"), ArgExample("-s 5 -d 6", "Desc1")]
    public void Copy(FileOperationTwoOperandArgs args)
    {
        Console.WriteLine("Copy");
    }

    [ArgActionMethod, ArgDescription("Deletes the specified saves.")]
    public void Delete(FileOperationOneOperandArgs args)
    {
        Console.WriteLine("Delete");
    }

    [ArgActionMethod, ArgDescription("Swaps the specified saves.")]
    public void Swap(FileOperationTwoOperandArgs args)
    {
        Console.WriteLine("Swap");
    }

    [ArgActionMethod, ArgDescription("Moves the specified saves.")]
    public void Move(FileOperationTwoOperandArgs args)
    {
        Console.WriteLine("Move");
    }
}