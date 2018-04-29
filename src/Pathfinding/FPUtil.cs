namespace Pathfinding
{
    // Floating point comparisons 
    public static class FPUtil
    {
        private const double kTolerance = 0.000001;    // floating point tolerance

        public static bool Less( double a, double b )
        {
            return ( a < b - kTolerance );
        }

        public static bool Greater( double a, double b )
        {
            return ( a > b + kTolerance );
        }

        public static bool GreaterEq( double a, double b )
        {
            return !Less( a, b );
        }

        public static bool Equal( double a, double b )
        {
            return ( a >= b - kTolerance ) && ( a <= b + kTolerance );
        }
    }
}