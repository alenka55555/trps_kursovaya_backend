namespace BooleanCompletenessBack
{
    // ClientException будет представлять из себя
    // ошибку, суть которой предполагаем отобразить пользователю
    // в противовес другим ошибкам, которые будут продуцировать
    // "Ошибку сервера", но логироваться на консоль бекенда.
    public class ClientException : Exception
    {
        public ClientException(string message) : base(message) {
            /* ничего */
        }
    }
}
