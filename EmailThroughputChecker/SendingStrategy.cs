namespace EmailThroughputChecker
{
    internal enum SendingStrategy
    {
        AmazonSesApiRaw = 1,
        AmazonSesApi,
        AmazonSesApiRawNoSdk,
        Smtp
    }
}