namespace RagPrototype.Infrastructure.VectorMath;

/// <summary>
/// [CRITERIO DE INGENIERÍA - DECISIÓN ARQUITECTÓNICA]
/// ¿Por qué esta lógica matemática vive en Infrastructure y no en Application?
/// 
/// Esta clase actúa como un "polyfill" para una base de datos vectorial. 
/// En un sistema de producción real (ej. Azure Cosmos DB, Qdrant, pgvector), 
/// el motor de la base de datos realiza la comparación vectorial nativamente 
/// utilizando índices optimizados (como HNSW o DiskANN).
/// 
/// Como en este prototipo estamos simulando la base de datos en RAM, 
/// esta matemática es un "detalle de implementación" de la persistencia y búsqueda, 
/// no una regla de negocio central (Core Domain/Application). 
/// Si mañana migramos a Cosmos DB, esta clase se eliminaría por completo sin 
/// afectar la capa de Aplicación.
/// </summary>
public static class CosineSimilarity
{
    /// <summary>
    /// Calcula la similitud coseno entre dos vectores (arrays de floats).
    /// Devuelve un valor entre -1 y 1, donde 1 significa que son idénticos.
    /// </summary>
    public static float Calculate(float[] a, float[] b)
    {
        if (a.Length != b.Length)
            throw new ArgumentException("Los vectores deben tener la misma dimensión para calcular su similitud.");

        float dotProduct = 0f;
        float magnitudeA = 0f;
        float magnitudeB = 0f;

        // Usamos un ciclo 'for' estándar en lugar de LINQ por rendimiento extremo,
        // ya que este cálculo se repite miles de veces en un escaneo lineal (Exact Nearest Neighbor).
        for (int i = 0; i < a.Length; i++)
        {
            dotProduct += a[i] * b[i];
            magnitudeA += a[i] * a[i];
            magnitudeB += b[i] * b[i];
        }

        float denominator = MathF.Sqrt(magnitudeA) * MathF.Sqrt(magnitudeB);

        return denominator == 0f ? 0f : dotProduct / denominator;
    }
}