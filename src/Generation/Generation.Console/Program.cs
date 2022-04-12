using Generation.Core;

var generator = new CombinationGenerator();
generator.MoveNext();
Console.WriteLine("Your ticket is: " + string.Join(", ", generator.Result));