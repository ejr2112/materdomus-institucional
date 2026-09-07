namespace MaterDomus.Web.Helpers;

/// <summary>
/// Fase do ciclo de carregamento de conteúdo.
/// </summary>
public enum LoadingPhase
{
    Initial,
    Prolonged,
    Success,
    Error
}

/// <summary>
/// Funções puras que isolam a lógica de decisão do estado de carregamento,
/// separada de I/O e da renderização, para permitir teste baseado em propriedades.
/// </summary>
public static class LoadingStateHelpers
{
    /// <summary>
    /// Mantém a contagem de placeholders no intervalo fechado [4, 12].
    /// Requisito 2.1.
    /// </summary>
    public static int ClampPlaceholderCount(int requested)
        => Math.Clamp(requested, 4, 12);

    /// <summary>
    /// Deriva a fase de carregamento a partir do tempo decorrido (em segundos) e do resultado.
    /// Requisitos 6.1, 6.3.
    /// </summary>
    /// <param name="elapsedSeconds">Tempo decorrido em segundos.</param>
    /// <param name="completed">Indica se o carregamento foi concluído com sucesso.</param>
    /// <param name="errored">Indica se o carregamento falhou.</param>
    public static LoadingPhase DerivePhase(double elapsedSeconds, bool completed, bool errored)
    {
        if (errored || (elapsedSeconds > 10 && !completed))
            return LoadingPhase.Error;

        if (completed)
            return LoadingPhase.Success;

        if (elapsedSeconds >= 3 && elapsedSeconds <= 10)
            return LoadingPhase.Prolonged;

        return LoadingPhase.Initial;
    }

    /// <summary>
    /// Valida uma mensagem de carregamento: verdadeiro quando não vazia e com no máximo 60 caracteres.
    /// Requisitos 1.3, 2.2.
    /// </summary>
    public static bool IsValidLoadingMessage(string message)
        => !string.IsNullOrEmpty(message) && message.Length <= 60;
}
