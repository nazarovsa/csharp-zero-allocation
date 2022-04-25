using Generation.Core;

var generator = new CombinationGenerator(10);
var combination = generator.Generate();
Console.WriteLine("Your ticket is: " + string.Join(", ", combination));

var efficientGenerator = new CombinationGeneratorEfficient(10);
efficientGenerator.MoveNext();
Console.WriteLine("Your ticket is: " + string.Join(", ", efficientGenerator.Result));