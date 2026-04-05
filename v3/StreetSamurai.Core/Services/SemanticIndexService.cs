using System.Text.RegularExpressions;

namespace StreetSamurai.Core.Services;

/// <summary>
/// TF-IDF semantic search over the world graph. Indexes all entity descriptions,
/// relationships, and properties. Enables finding thematically relevant entities
/// even when exact names aren't mentioned — e.g. searching "corporate betrayal"
/// surfaces Sable's backstory.
/// </summary>
public class SemanticIndexService
{
    private readonly WorldGraphService graph;

    // TF-IDF vectors: nodeId -> (term -> weight)
    private Dictionary<string, Dictionary<string, double>> _vectors = new();
    // Inverse document frequency per term
    private Dictionary<string, double> _idf = new();
    // Total indexed documents
    private int docCount;
    private bool built;

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for", "of", "with",
        "by", "from", "is", "it", "its", "that", "this", "was", "are", "be", "been", "being",
        "have", "has", "had", "do", "does", "did", "will", "would", "could", "should", "may",
        "might", "can", "shall", "not", "no", "nor", "as", "if", "then", "than", "when",
        "where", "which", "who", "whom", "what", "how", "all", "each", "every", "both",
        "few", "more", "most", "other", "some", "such", "only", "own", "same", "so",
        "very", "just", "because", "about", "into", "through", "during", "before", "after",
        "above", "below", "between", "out", "off", "over", "under", "again", "further",
        "once", "here", "there", "also", "too", "they", "them", "their", "he", "she", "him",
        "her", "his", "we", "our", "you", "your", "up", "any", "much", "many", "like"
    };

    public SemanticIndexService(WorldGraphService graph)
    {
        this.graph = graph;
    }

    public int IndexedCount => docCount;
    public bool IsBuilt => built;

    /// <summary>
    /// Build or rebuild the full TF-IDF index from all graph nodes.
    /// </summary>
    public void RebuildIndex()
    {
        _vectors.Clear();
        _idf.Clear();

        var allNodes = graph.AllNodes();
        var documents = new Dictionary<string, List<string>>(); // nodeId -> tokens

        // Build documents from node properties
        foreach (var node in allNodes)
        {
            if (string.IsNullOrWhiteSpace(node.Id)) continue;

            var text = new System.Text.StringBuilder();
            text.Append(node.Name).Append(' ');
            text.Append(node.NodeType).Append(' ');

            foreach (var (key, value) in node.Properties)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    text.Append(value).Append(' ');
            }

            // Include edge descriptions for richer context
            var edges = graph.GetAllEdges(node.Id);
            foreach (var edge in edges)
            {
                if (!string.IsNullOrWhiteSpace(edge.Description))
                    text.Append(edge.Description).Append(' ');
                text.Append(edge.RelationType).Append(' ');
            }

            var tokens = Tokenize(text.ToString());
            if (tokens.Count > 0)
                documents[node.Id] = tokens;
        }

        docCount = documents.Count;
        if (docCount == 0) { built = true; return; }

        // Compute document frequency per term
        var df = new Dictionary<string, int>();
        foreach (var (_, tokens) in documents)
        {
            foreach (var term in tokens.Distinct())
            {
                df[term] = df.GetValueOrDefault(term, 0) + 1;
            }
        }

        // Compute IDF
        foreach (var (term, count) in df)
        {
            _idf[term] = Math.Log((double)docCount / (1 + count));
        }

        // Compute TF-IDF vectors
        foreach (var (nodeId, tokens) in documents)
        {
            var termCounts = new Dictionary<string, int>();
            foreach (var t in tokens)
                termCounts[t] = termCounts.GetValueOrDefault(t, 0) + 1;

            var maxTf = termCounts.Values.Max();
            var vector = new Dictionary<string, double>();
            foreach (var (term, count) in termCounts)
            {
                var tf = 0.5 + 0.5 * count / maxTf; // augmented TF to prevent bias toward long docs
                vector[term] = tf * _idf.GetValueOrDefault(term, 0);
            }
            _vectors[nodeId] = vector;
        }

        built = true;
    }

    /// <summary>
    /// Incrementally update the index for a single node (after edit/add).
    /// </summary>
    public void UpdateNode(string nodeId)
    {
        if (!built) { RebuildIndex(); return; }

        var node = graph.GetNode(nodeId);
        if (node == null) { _vectors.Remove(nodeId); return; }

        var text = new System.Text.StringBuilder();
        text.Append(node.Name).Append(' ').Append(node.NodeType).Append(' ');
        foreach (var (_, value) in node.Properties)
            if (!string.IsNullOrWhiteSpace(value)) text.Append(value).Append(' ');
        foreach (var edge in graph.GetAllEdges(nodeId))
        {
            if (!string.IsNullOrWhiteSpace(edge.Description)) text.Append(edge.Description).Append(' ');
            text.Append(edge.RelationType).Append(' ');
        }

        var tokens = Tokenize(text.ToString());
        if (tokens.Count == 0) { _vectors.Remove(nodeId); return; }

        var termCounts = new Dictionary<string, int>();
        foreach (var t in tokens) termCounts[t] = termCounts.GetValueOrDefault(t, 0) + 1;
        var maxTf = termCounts.Values.Max();

        var vector = new Dictionary<string, double>();
        foreach (var (term, count) in termCounts)
        {
            var tf = 0.5 + 0.5 * count / maxTf;
            vector[term] = tf * _idf.GetValueOrDefault(term, 0);
        }
        _vectors[nodeId] = vector;
    }

    /// <summary>
    /// Search for nodes semantically similar to the query text.
    /// Returns node IDs ranked by cosine similarity.
    /// </summary>
    public List<(string nodeId, double score)> Search(string query, int topK = 10)
    {
        if (!built) RebuildIndex();

        var queryTokens = Tokenize(query);
        if (queryTokens.Count == 0) return [];

        // Build query vector
        var queryCounts = new Dictionary<string, int>();
        foreach (var t in queryTokens) queryCounts[t] = queryCounts.GetValueOrDefault(t, 0) + 1;
        var maxQf = queryCounts.Values.Max();

        var queryVector = new Dictionary<string, double>();
        foreach (var (term, count) in queryCounts)
        {
            if (!_idf.ContainsKey(term)) continue;
            var tf = 0.5 + 0.5 * count / maxQf;
            queryVector[term] = tf * _idf[term];
        }

        if (queryVector.Count == 0) return [];

        // Cosine similarity against all documents
        var results = new List<(string nodeId, double score)>();
        foreach (var (nodeId, docVector) in _vectors)
        {
            var score = CosineSimilarity(queryVector, docVector);
            if (score > 0.01) // threshold to filter noise
                results.Add((nodeId, score));
        }

        return results.OrderByDescending(r => r.score).Take(topK).ToList();
    }

    /// <summary>
    /// Get terms that are most distinctive for a given node (for diagnostics/UI).
    /// </summary>
    public List<(string term, double weight)> GetTopTerms(string nodeId, int topK = 10)
    {
        if (!_vectors.TryGetValue(nodeId, out var vector)) return [];
        return vector.OrderByDescending(kv => kv.Value).Take(topK)
            .Select(kv => (kv.Key, kv.Value)).ToList();
    }

    private static double CosineSimilarity(Dictionary<string, double> a, Dictionary<string, double> b)
    {
        double dot = 0, magA = 0, magB = 0;
        foreach (var (term, weight) in a)
        {
            magA += weight * weight;
            if (b.TryGetValue(term, out var bWeight))
                dot += weight * bWeight;
        }
        foreach (var (_, weight) in b)
            magB += weight * weight;

        if (magA == 0 || magB == 0) return 0;
        return dot / (Math.Sqrt(magA) * Math.Sqrt(magB));
    }

    private static List<string> Tokenize(string text)
    {
        return Regex.Split(text.ToLowerInvariant(), @"[^a-z0-9]+")
            .Where(t => t.Length > 2 && !StopWords.Contains(t))
            .ToList();
    }
}
