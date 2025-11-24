namespace NomadInjector
{
    public enum InjectionMethod
    {
        Standard,
        ManualMap,
        ThreadHijacking
    }

    public static class GlobalSettings
    {
        public static InjectionMethod CurrentMethod { get; set; } = InjectionMethod.Standard;
        public static bool ErasePEHeaders { get; set; } = false;
        public static bool HideModule { get; set; } = false;
        public static int ThreadTimeout { get; set; } = 5000;

    }
}