using System;
using System.Collections.Generic;
using System.Linq;

namespace Pathfinding
{
    public enum StateLocation
    {
        OpenReady = 0, //priority queue 0, low g -> low f
        OpenWaiting = 1, //priority queue 1, low f -> low g
        Closed,
        Unseen
    }

    public class BDOpenClosedData<TState>
    {
        public readonly TState Data;
        public double G;
        public readonly double H;
        public int OpenLocation;
        public int ParentID;
        public StateLocation Where;
        public bool Reopened;

        public BDOpenClosedData( TState theData, double gCost, double hCost, int parent, int openLoc, StateLocation location )
        {
            Data = theData;
            G = gCost;
            H = hCost;
            ParentID = parent;
            OpenLocation = openLoc;
            Where = location;
            Reopened = false;
        }
    }

    public class BDOpenClosed<TState>
    {
        private const int kTBDNoNode = int.MaxValue;
        //all the elements, open or closed
        private readonly List<BDOpenClosedData<TState>> _elements;

        //2 queues:
        //priorityQueues[0] is openReady, priorityQueues[1] is openWaiting
        private readonly List<int>[] _priorityQueues;

        private readonly Dictionary<int, int> _table;

        public BDOpenClosed()
        {
            _priorityQueues = new[]
            {
                new List<int>(),
                new List<int>()
            };
            _table = new Dictionary<int, int>();
            _elements = new List<BDOpenClosedData<TState>>();
        }

        public void Reset()
        {
            _table.Clear();
            _elements.Clear();
            _priorityQueues[0].Clear();
            _priorityQueues[1].Clear();
        }

        public int AddOpenNode( TState val, int hash, double g, double h, int parent = kTBDNoNode,
            StateLocation whichQueue = StateLocation.OpenWaiting )
        {
            // should do lookup here...
            if( _table.ContainsKey( hash ) )
            {
                throw new InvalidOperationException( "Cannot add the same open node twice" );
            }
            if( whichQueue == StateLocation.OpenReady )
            {
                _elements.Add( new BDOpenClosedData<TState>( val, g, h, parent,
                    _priorityQueues[0].Count, StateLocation.OpenReady ) );
            }
            else if( whichQueue == StateLocation.OpenWaiting )
            {
                _elements.Add( new BDOpenClosedData<TState>( val, g, h, parent,
                    _priorityQueues[1].Count, StateLocation.OpenWaiting ) );
            }

            if( parent == kTBDNoNode )
            {
                _elements.Last().ParentID = _elements.Count - 1;
            }
            _table[hash] = _elements.Count - 1; // hashing to element list location

            _priorityQueues[(int) whichQueue].Add( _elements.Count - 1 );
            HeapifyUp( _priorityQueues[(int) whichQueue].Count - 1, (int) whichQueue );

            return _elements.Count - 1;
        }

        public int AddClosedNode( TState val, int hash, double g, double h, int parent = kTBDNoNode )
        {
            _elements.Add( new BDOpenClosedData<TState>( val, g, h, parent, 0, StateLocation.Closed ) );
            if( parent == kTBDNoNode )
            {
                _elements.Last().ParentID = _elements.Count - 1;
            }
            _table[hash] = _elements.Count - 1; // hashing to element list location
            return _elements.Count - 1;
        }

        public void KeyChanged( int val )
        {
            if( _elements[val].Where == StateLocation.OpenReady )
            {
                if( !HeapifyUp( _elements[val].OpenLocation, (int) StateLocation.OpenReady ) )
                {
                    HeapifyDown( _elements[val].OpenLocation, (int) StateLocation.OpenReady );
                }
            }
            else if( _elements[val].Where == StateLocation.OpenWaiting )
            {
                if( !HeapifyUp( _elements[val].OpenLocation, (int) StateLocation.OpenWaiting ) )
                {
                    HeapifyDown( _elements[val].OpenLocation, (int) StateLocation.OpenWaiting );
                }
            }
        }

        public void Remove( int val )
        {
            int index = _elements[val].OpenLocation;
            int whichQueue = (int) _elements[val].Where;
            _elements[val].Where = StateLocation.Closed;
            _priorityQueues[whichQueue][index] = _priorityQueues[whichQueue][_priorityQueues[whichQueue].Count - 1];
            _elements[_priorityQueues[whichQueue][index]].OpenLocation = index;
            _priorityQueues[whichQueue].PopBack();

            if( !HeapifyUp( index, whichQueue ) )
            {
                HeapifyDown( index, whichQueue );
            }
        }

        public StateLocation Lookup( int hashKey, out int objKey )
        {
            if( _table.ContainsKey( hashKey ) )
            {
                objKey = _table[hashKey];
                return _elements[objKey].Where;
            }
            objKey = -1;
            return StateLocation.Unseen;
        }

        public BDOpenClosedData<TState> Lookup( int objKey )
        {
            return _elements[objKey];
        }

        public BDOpenClosedData<TState> Lookat( int objKey )
        {
            return _elements[objKey];
        }

        public int Peek( StateLocation whichQueue )
        {
            if( whichQueue == StateLocation.OpenReady )
            {
                if( OpenReadySize() == 0 )
                {
                    throw new InvalidOperationException( "cannot peek at empty ready queue" );
                }
            }
            else if( whichQueue == StateLocation.OpenWaiting )
            {
                if( OpenWaitingSize() == 0 )
                {
                    throw new InvalidOperationException( "cannot peek at empty waiting queue" );
                }
            }
            return _priorityQueues[(int) whichQueue][0];
        }

        public BDOpenClosedData<TState> PeekAt( StateLocation whichQueue )
        {
            if( whichQueue == StateLocation.OpenReady )
            {
                if( OpenReadySize() == 0 )
                {
                    throw new InvalidOperationException( "cannot peek at empty ready queue" );
                }
            }
            else if( whichQueue == StateLocation.OpenWaiting )
            {
                if( OpenWaitingSize() == 0 )
                {
                    throw new InvalidOperationException( "cannot peek at empty waiting queue" );
                }
            }
            return _elements[_priorityQueues[(int) whichQueue][0]];
        }

        public int Close()
        {
            if( OpenReadySize() == 0 )
            {
                throw new InvalidOperationException( "cannot peek at empty ready queue" );
            }

            int ans = _priorityQueues[0][0];
            _elements[ans].Where = StateLocation.Closed;
            _priorityQueues[0][0] = _priorityQueues[0][OpenReadySize() - 1];
            _elements[_priorityQueues[0][0]].OpenLocation = 0;
            _priorityQueues[0].PopBack();

            HeapifyDown( 0, (int) StateLocation.OpenReady );

            return ans;
        }

        public int PutToReady()
        {
            if( OpenWaitingSize() == 0 )
            {
                throw new InvalidOperationException( "cannot peek at empty waiting queue" );
            }

            //remove it from openWaiting
            int ans = _priorityQueues[(int) StateLocation.OpenWaiting][0];
            int back = _priorityQueues[(int) StateLocation.OpenWaiting].Last();
            _priorityQueues[(int) StateLocation.OpenWaiting][0] = back;
            _priorityQueues[(int) StateLocation.OpenWaiting].PopBack();
            _elements[back].OpenLocation = 0;

            HeapifyDown( 0, (int) StateLocation.OpenWaiting );

            //put it to openReady
            _priorityQueues[(int) StateLocation.OpenReady].Add( ans );
            _elements[ans].Where = StateLocation.OpenReady;
            _elements[ans].OpenLocation = _priorityQueues[(int) StateLocation.OpenReady].Count - 1;

            HeapifyUp( _priorityQueues[(int) StateLocation.OpenReady].Count - 1, (int) StateLocation.OpenReady );

            return ans;
        }

        public int GetOpenItem( int which, StateLocation where )
        {
            return _priorityQueues[(int) where][which];
        }

        public int OpenReadySize()
        {
            return _priorityQueues[(int) StateLocation.OpenReady].Count;
        }

        public int OpenWaitingSize()
        {
            return _priorityQueues[(int) StateLocation.OpenWaiting].Count;
        }

        public int OpenSize()
        {
            return _priorityQueues[(int) StateLocation.OpenReady].Count +
                 _priorityQueues[(int) StateLocation.OpenWaiting].Count;
        }

        public int ClosedSize()
        {
            return Size() - OpenReadySize() - OpenWaitingSize();
        }

        public int Size()
        {
            return _elements.Count;
        }

        public void VerifyData()
        {
        }

        public bool ValidateOpenReady( int index = 0 )
        {
            int whichQ = (int) StateLocation.OpenReady;
            if( index >= _priorityQueues[whichQ].Count )
            {
                return true;
            }
            int child1 = index * 2 + 1;
            int child2 = index * 2 + 2;
            if( _priorityQueues[whichQ].Count > child1 &&
                !CompareOpenReady( _elements[_priorityQueues[whichQ][child1]], _elements[_priorityQueues[whichQ][index]] ) )
            {
                return false;
            }
            if( _priorityQueues[whichQ].Count > child2 &&
                !CompareOpenReady( _elements[_priorityQueues[whichQ][child2]], _elements[_priorityQueues[whichQ][index]] ) )
            {
                return false;
            }
            return ValidateOpenReady( child1 ) && ValidateOpenReady( child2 );
        }

        public bool ValidateOpenWaiting( int index = 0 )
        {
            int whichQ = (int) StateLocation.OpenWaiting;
            if( index >= _priorityQueues[whichQ].Count )
            {
                return true;
            }
            int child1 = index * 2 + 1;
            int child2 = index * 2 + 2;
            if( _priorityQueues[whichQ].Count > child1 &&
                !CompareOpenWaiting( _elements[_priorityQueues[whichQ][child1]], _elements[_priorityQueues[whichQ][index]] ) )
            {
                return false;
            }
            if( _priorityQueues[whichQ].Count > child2 &&
                !CompareOpenWaiting( _elements[_priorityQueues[whichQ][child2]], _elements[_priorityQueues[whichQ][index]] ) )
            {
                return false;
            }
            return ValidateOpenWaiting( child1 ) && ValidateOpenWaiting( child2 );
        }

        private bool HeapifyUp( int index, int whichQueue )
        {
            if( index == 0 )
            {
                return false;
            }
            int parent = ( index - 1 ) / 2;

            if( whichQueue == (int) StateLocation.OpenReady )
            {
                if( CompareOpenReady( _elements[_priorityQueues[whichQueue][parent]],
                    _elements[_priorityQueues[whichQueue][index]] ) )
                {
                    int tmp = _priorityQueues[whichQueue][parent];
                    _priorityQueues[whichQueue][parent] = _priorityQueues[whichQueue][index];
                    _priorityQueues[whichQueue][index] = tmp;
                    _elements[_priorityQueues[whichQueue][parent]].OpenLocation = parent;
                    _elements[_priorityQueues[whichQueue][index]].OpenLocation = index;
                    HeapifyUp( parent, whichQueue );
                    return true;
                }
            }
            else if( whichQueue == (int) StateLocation.OpenWaiting )
            {
                if( CompareOpenWaiting( _elements[_priorityQueues[whichQueue][parent]],
                    _elements[_priorityQueues[whichQueue][index]] ) )
                {
                    int tmp = _priorityQueues[whichQueue][parent];
                    _priorityQueues[whichQueue][parent] = _priorityQueues[whichQueue][index];
                    _priorityQueues[whichQueue][index] = tmp;
                    _elements[_priorityQueues[whichQueue][parent]].OpenLocation = parent;
                    _elements[_priorityQueues[whichQueue][index]].OpenLocation = index;
                    HeapifyUp( parent, whichQueue );
                    return true;
                }
            }

            return false;
        }

        private void HeapifyDown( int index, int whichQueue )
        {
            int child1 = index * 2 + 1;
            int child2 = index * 2 + 2;

            if( whichQueue == (int) StateLocation.OpenReady )
            {
                int which;
                int count = _priorityQueues[whichQueue].Count;
                // find smallest child
                if( child1 >= count )
                {
                    return;
                }
                if( child2 >= count )
                {
                    which = child1;
                }
                else if( !CompareOpenReady( _elements[_priorityQueues[whichQueue][child1]],
                    _elements[_priorityQueues[whichQueue][child2]] ) )
                {
                    which = child1;
                }
                else
                {
                    which = child2;
                }

                if( !CompareOpenReady( _elements[_priorityQueues[whichQueue][which]],
                    _elements[_priorityQueues[whichQueue][index]] ) )
                {
                    int tmp = _priorityQueues[whichQueue][which];
                    _priorityQueues[whichQueue][which] = _priorityQueues[whichQueue][index];
                    _priorityQueues[whichQueue][index] = tmp;
                    _elements[_priorityQueues[whichQueue][which]].OpenLocation = which;
                    _elements[_priorityQueues[whichQueue][index]].OpenLocation = index;
                    HeapifyDown( which, whichQueue );
                }
            }
            else if( whichQueue == (int) StateLocation.OpenWaiting )
            {
                int which;
                int count = _priorityQueues[whichQueue].Count;
                // find smallest child
                if( child1 >= count ) // no children; done
                {
                    return;
                }
                if( child2 >= count ) // one child - compare there
                {
                    which = child1;
                }
                // find larger child to move up
                else if( !CompareOpenWaiting( _elements[_priorityQueues[whichQueue][child1]],
                    _elements[_priorityQueues[whichQueue][child2]] ) )
                {
                    which = child1;
                }
                else
                {
                    which = child2;
                }

                if( !CompareOpenWaiting( _elements[_priorityQueues[whichQueue][which]],
                    _elements[_priorityQueues[whichQueue][index]] ) )
                {
                    int tmp = _priorityQueues[whichQueue][which];
                    _priorityQueues[whichQueue][which] = _priorityQueues[whichQueue][index];
                    _priorityQueues[whichQueue][index] = tmp;

                    _elements[_priorityQueues[whichQueue][which]].OpenLocation = which;
                    _elements[_priorityQueues[whichQueue][index]].OpenLocation = index;
                    HeapifyDown( which, whichQueue );
                }
            }
        }

        //low g -> low f
        public static bool CompareOpenReady( BDOpenClosedData<TState> i1, BDOpenClosedData<TState> i2 )
        {
            double f1 = i1.G + i1.H;
            double f2 = i2.G + i2.H;

            if( FPUtil.Equal( i1.G, i2.G ) )
            {
                return !FPUtil.Less( f1, f2 );
            }
            return FPUtil.Greater( i1.G, i2.G ); // low g over high
        }

        public static bool CompareOpenWaiting( BDOpenClosedData<TState> i1, BDOpenClosedData<TState> i2 )
        {
            double f1 = i1.G + i1.H;
            double f2 = i2.G + i2.H;

            if( FPUtil.Equal( f1, f2 ) )
            {
                return !FPUtil.Greater( i1.G, i2.G );
            }
            return FPUtil.Greater( f1, f2 ); // low f over high
        }
    }

    public static class ListExtentions
    {
        public static void PopBack<T>( this List<T> list )
        {
            list.RemoveAt( list.Count - 1 );
        }
    }
}