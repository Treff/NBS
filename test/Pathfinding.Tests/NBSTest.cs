using System;
using System.Collections.Generic;
using Xunit;

namespace Pathfinding.Tests
{
    public class NBSTest
    {
        [Fact]
        public void TestLine()
        {
            NBS<int> nbs = new NBS<int>();
            Assert.NotNull( nbs );

            int[] GetSuccessors( int n )
            {
                if( n == 0 ) return new[] { 1 };
                if( n == 1 ) return new[] { 0, 2 };
                return new[] { 1 };
            }
            List<int> thePath = new List<int>();
            nbs.GetPath( 0, 2, n => n, GetSuccessors, ( s, e ) => 1, ( s, e ) => 1, ( s, e ) => 1, thePath );

            Assert.Collection( thePath,
                n => Assert.Equal( 0, n ),
                n => Assert.Equal( 1, n ),
                n => Assert.Equal( 2, n ) );
        }

        [Fact]
        public void TestGrid()
        {
            NBS<int> nbs = new NBS<int>();
            Assert.NotNull( nbs );

            int[,] grid = new int[10, 10];
            int index = 0;
            for( int i = 0; i < 10; i++ )
            {
                for( int j = 0; j < 10; j++ )
                {
                    grid[j, i] = index++;
                }
            }
            int goalStart = grid[1, 1];
            int goalEnd = grid[8, 8];
            int[] GetSuccessors( int start )
            {
                List<int> neighbours = new List<int>();
                int x = start % 10;
                int y = start / 10;
                if( x != 0 )
                {
                    neighbours.Add( grid[x - 1, y] );
                }
                if( y != 0 )
                {
                    neighbours.Add( grid[x, y - 1] );
                }
                if( x != 9 )
                {
                    neighbours.Add( grid[x + 1, y] );
                }
                if( y != 9 )
                {
                    neighbours.Add( grid[x, y + 1] );
                }
                return neighbours.ToArray();
            }
            double GetGCost( int start, int end )
            {
                int startX = start % 10;
                int startY = start / 10;
                int endX = end % 10;
                int endY = end / 10;
                int deltaX = endX - startX;
                int deltaY = endY - startY;
                return Math.Abs( deltaX ) + Math.Abs( deltaY );
            }
            double GetHCost( int start, int end )
            {
                int startX = start % 10;
                int startY = start / 10;
                int endX = end % 10;
                int endY = end / 10;
                int deltaX = endX - startX;
                int deltaY = endY - startY;
                return Math.Sqrt( ( deltaX * deltaX ) + ( deltaY * deltaY ) );
            }

            List<int> thePath = new List<int>();
            nbs.GetPath( goalStart, goalEnd, n => n, GetSuccessors, GetGCost, GetHCost, GetHCost, thePath );

            Assert.Collection( thePath,
                n => Assert.Equal( 11, n ),
                n => Assert.Equal( 21, n ),
                n => Assert.Equal( 22, n ),
                n => Assert.Equal( 23, n ),
                n => Assert.Equal( 33, n ),
                n => Assert.Equal( 43, n ),
                n => Assert.Equal( 44, n ),
                n => Assert.Equal( 54, n ),
                n => Assert.Equal( 55, n ),
                n => Assert.Equal( 56, n ),
                n => Assert.Equal( 66, n ),
                n => Assert.Equal( 76, n ),
                n => Assert.Equal( 77, n ),
                n => Assert.Equal( 78, n ),
                n => Assert.Equal( 88, n ) );
        }
    }
}