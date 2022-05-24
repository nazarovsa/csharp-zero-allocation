var guid = Guid.NewGuid();
Console.WriteLine("New guid: {0}", guid);

var effectiveStringFromGuid = GuidTransformer.Core.GuidTransformer.ToStringFromGuid(guid);
Console.WriteLine("Effective string from guid: {0}", effectiveStringFromGuid);

var parsedGuid = GuidTransformer.Core.GuidTransformer.ToGuidFromString(effectiveStringFromGuid);
Console.WriteLine("Parsed Guid: {0}", parsedGuid);

var efficientGuidString = GuidTransformer.Core.GuidTransformer.ToStringFromGuidEfficient(guid);
Console.WriteLine("Efficient effective string from guid: {0}", efficientGuidString);

var parsedEfficientGuid = GuidTransformer.Core.GuidTransformer.ToGuidFromStringEfficient(efficientGuidString);
Console.WriteLine("Efficient Parsed Guid: {0}", parsedEfficientGuid);