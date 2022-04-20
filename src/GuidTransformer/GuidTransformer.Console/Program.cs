var guid = Guid.NewGuid();
Console.WriteLine("New guid: {0}", guid);

var efficientGuidString = GuidTransformer.Core.GuidTransformer.ToStringFromGuid(guid);
Console.WriteLine("Efficient Guid String: {0}", efficientGuidString);

var parsedGuid = GuidTransformer.Core.GuidTransformer.ToGuidFromString(efficientGuidString);
Console.WriteLine("Parsed Guid: {0}", parsedGuid);