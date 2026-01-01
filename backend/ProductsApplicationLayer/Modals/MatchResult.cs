using Qdrant.Client.Grpc;

namespace ProductsApplicationLayer.ViewModals;

public class LocalProduct {
    public string Id { get; set; }
    public string Name { get; set; }
    public double SearchScore { get; set; } // Temporary storage for vector similarity
}

public class MatchResult {
    public string ReceiptName { get; set; }
    public ScoredPoint MatchedProduct { get; set; }
    public double VectorScore { get; set; }
    public double FuzzyScore { get; set; }
    public double FinalScore { get; set; }
}