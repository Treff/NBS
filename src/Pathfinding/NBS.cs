using System;
using System.Collections.Generic;
using System.Linq;

namespace Pathfinding
{
    public delegate int GetStateHash<in TState>( TState state );
    public delegate IEnumerable<TState> GetSuccessors<TState>( TState start );
    public delegate double GCost<in TState>( TState start, TState end );
    public delegate double HCost<in TState>( TState start, TState end );

    public class NBS<TState> where TState : IEquatable<TState>
    {
        private readonly Dictionary<double, int> _counts;
        private readonly NBSQueue<TState> _queue;
        private HCost<TState> _backwardHeuristic;
        private double _currentCost;
        private HCost<TState> _forwardHeuristic;
        private GCost<TState> _gCost;

        private GetStateHash<TState> _getStateHash;
        private GetSuccessors<TState> _getSuccessors;
        private TState _goal, _start;
        private TState _middleNode;

        private int _nodesTouched, _nodesExpanded;

        public NBS( double epsilon = 1d )
        {
            _forwardHeuristic = null;
            _backwardHeuristic = null;
            _counts = new Dictionary<double, int>();
            _queue = new NBSQueue<TState>( epsilon );
            ResetNodeCount();
        }

        public void GetPath( TState from, TState to, GetStateHash<TState> getHash, GetSuccessors<TState> getSuccessors, 
            GCost<TState> gCost, HCost<TState> forward, HCost<TState> backward, List<TState> thePath )
        {
            if( InitializeSearch( from, to, getHash, getSuccessors, gCost, forward, backward, thePath ) == false )
            {
                return;
            }

            while( !ExpandAPair( thePath ) )
            {
            }
        }

        public bool InitializeSearch( TState from, TState to, GetStateHash<TState> getHash, GetSuccessors<TState> getSuccessors,
            GCost<TState> gCost, HCost<TState> forward, HCost<TState> backward, List<TState> thePath )
        {
            _getStateHash = getHash;
            _gCost = gCost;
            _getSuccessors = getSuccessors;
            _forwardHeuristic = forward;
            _backwardHeuristic = backward;
            _currentCost = double.MaxValue;
            _queue.Reset();
            ResetNodeCount();
            thePath.Clear();
            _start = from;
            _goal = to;

            if( _start.Equals( _goal ) )
            {
                return false;
            }
            _queue.ForwardQueue.AddOpenNode( _start, _getStateHash( _start ), 0, _forwardHeuristic( _start, _goal ) );
            _queue.BackwardQueue.AddOpenNode( _goal, _getStateHash( _goal ), 0, _backwardHeuristic( _goal, _start ) );
            return true;
        }

        public bool ExpandAPair( List<TState> thePath )
        {
            bool result = _queue.GetNextPair( out int nForward, out int nBackward );
            // if failed, see if we have optimal path (but return)
            if( result == false )
            {
                if( _currentCost == double.MaxValue )
                {
                    thePath.Clear();
                    return true;
                }
                ExtractFromMiddle( thePath );
                return true;
            }
            // if success, see if nodes are the same (return path)
            if( _queue.ForwardQueue.Lookup( nForward ).Data.Equals( _queue.BackwardQueue.Lookup( nBackward ).Data ) )
            {
                if( _queue.TerminateOnG() )
                {
                    Console.WriteLine( "NBS: Lower Bound on C* from g+g (gsum)\n" );
                }
                ExtractFromMiddle( thePath );
                return true;
            }
            if( !FPUtil.Less( _queue.GetLowerBound(), _currentCost ) )
            {
                ExtractFromMiddle( thePath );
                return true;
            }

            double lowerBound = _queue.GetLowerBound();
            if( !_counts.ContainsKey( lowerBound ) )
            {
                _counts[lowerBound] = 0;
            }
            _counts[lowerBound] += 2;
            Expand( nForward, _queue.ForwardQueue, _queue.BackwardQueue, _forwardHeuristic, _goal );
            Expand( nBackward, _queue.BackwardQueue, _queue.ForwardQueue, _backwardHeuristic, _start );
            return false;
        }

        public bool DoSingleSearchStep( List<TState> thePath )
        {
            return ExpandAPair( thePath );
        }

        public void ResetNodeCount()
        {
            _nodesExpanded = _nodesTouched = 0;
            _counts.Clear();
        }

        public int GetNumForwardItems()
        {
            return _queue.ForwardQueue.Size();
        }

        public BDOpenClosedData<TState> GetForwardItem( int which )
        {
            return _queue.ForwardQueue.Lookat( which );
        }

        public int GetNumBackwardItems()
        {
            return _queue.BackwardQueue.Size();
        }

        public BDOpenClosedData<TState> GetBackwardItem( int which )
        {
            return _queue.BackwardQueue.Lookat( which );
        }

        public void SetForwardHeuristic( HCost<TState> h )
        {
            _forwardHeuristic = h;
        }

        public void SetBackwardHeuristic( HCost<TState> h )
        {
            _backwardHeuristic = h;
        }

        public StateLocation GetNodeForwardLocation( TState s )
        {
            return _queue.ForwardQueue.Lookup( _getStateHash( s ), out int childID );
        }

        public StateLocation GetNodeBackwardLocation( TState s )
        {
            return _queue.BackwardQueue.Lookup( _getStateHash( s ), out int childID );
        }

        public double GetNodeForwardG( TState s )
        {
            StateLocation l = _queue.ForwardQueue.Lookup( _getStateHash( s ), out int childID );
            if( l != StateLocation.Unseen )
            {
                return _queue.ForwardQueue.Lookat( childID ).G;
            }
            return -1;
        }

        public double GetNodeBackwardG( TState s )
        {
            StateLocation l = _queue.BackwardQueue.Lookup( _getStateHash( s ), out int childID );
            if( l != StateLocation.Unseen )
            {
                return _queue.BackwardQueue.Lookat( childID ).G;
            }
            return -1;
        }

        public int GetNodesExpanded()
        {
            return _nodesExpanded;
        }

        public int GetNodesTouched()
        {
            return _nodesTouched;
        }

        public int GetDoubleExpansions()
        {
            int doubles = 0;
            for( int x = 0; x < _queue.ForwardQueue.Size(); x++ )
            {
                int key;
                BDOpenClosedData<TState> data = _queue.ForwardQueue.Lookat( x );
                if( data.Where == StateLocation.Closed )
                {
                    StateLocation loc = _queue.BackwardQueue.Lookup( _getStateHash( data.Data ), out key );
                    if( loc == StateLocation.Closed )
                    {
                        doubles++;
                    }
                }
            }
            return doubles;
        }

        public int GetNecessaryExpansions()
        {
            int necessary = 0;
            foreach( KeyValuePair<double, int> i in _counts )
            {
                if( i.Key < _currentCost )
                {
                    necessary += i.Value;
                }
            }
            return necessary;
        }

        // returns 0...1 for the percentage of the optimal path length on each frontier
        public double GetMeetingPoint()
        {
            _queue.BackwardQueue.Lookup( _getStateHash( _middleNode ), out int bID );
            _queue.ForwardQueue.Lookup( _getStateHash( _middleNode ), out int fID );
            return _queue.BackwardQueue.Lookup( bID ).G / _currentCost;
        }

        public double GetSolutionCost()
        {
            return _currentCost;
        }

        private void ExtractFromMiddle( List<TState> thePath )
        {
            List<TState> pFor = new List<TState>();
            List<TState> pBack = new List<TState>();
            ExtractPathToGoal( _middleNode, pBack );
            ExtractPathToStart( _middleNode, pFor );
            pFor = Enumerable.Reverse( pFor ).ToList();
            thePath.Clear();
            thePath.AddRange( pFor );
            thePath.AddRange( pBack.Skip( 1 ) );
        }

        private void ExtractPathToGoal( TState node, List<TState> thePath )
        {
            _queue.BackwardQueue.Lookup( _getStateHash( node ), out int theID );
            ExtractPathToGoalFromID( theID, thePath );
        }

        private void ExtractPathToGoalFromID( int node, List<TState> thePath )
        {
            do
            {
                thePath.Add( _queue.BackwardQueue.Lookup( node ).Data );
                node = _queue.BackwardQueue.Lookup( node ).ParentID;
            } while( _queue.BackwardQueue.Lookup( node ).ParentID != node );
            thePath.Add( _queue.BackwardQueue.Lookup( node ).Data );
        }

        private void ExtractPathToStart( TState node, List<TState> thePath )
        {
            StateLocation loc = _queue.ForwardQueue.Lookup( _getStateHash( node ), out int theID );
            ExtractPathToStartFromID( theID, thePath );
        }

        private void ExtractPathToStartFromID( int node, List<TState> thePath )
        {
            do
            {
                thePath.Add( _queue.ForwardQueue.Lookup( node ).Data );
                node = _queue.ForwardQueue.Lookup( node ).ParentID;
            } while( _queue.ForwardQueue.Lookup( node ).ParentID != node );
            thePath.Add( _queue.ForwardQueue.Lookup( node ).Data );
        }

        private void Expand( int nextID, BDOpenClosed<TState> current, 
		  BDOpenClosed<TState> opposite, HCost<TState> heuristic, TState target )
        {
            current.Close();

            //this can happen when we expand a single node instead of a pair
            if( FPUtil.GreaterEq( current.Lookup( nextID ).G + current.Lookup( nextID ).H, _currentCost ) )
            {
                return;
            }

            _nodesExpanded++;
            IEnumerable<TState> neighbors = _getSuccessors( current.Lookup( nextID ).Data );
            foreach( TState succ in neighbors )
            {
                _nodesTouched++;
                StateLocation loc = current.Lookup( _getStateHash( succ ), out int childID );

                // screening
                double edgeCost = _gCost( current.Lookup( nextID ).Data, succ );
                if( FPUtil.GreaterEq( current.Lookup( nextID ).G + edgeCost, _currentCost ) )
                {
                    continue;
                }

                switch( loc )
                {
                    case StateLocation.Closed: // ignore
                        break;
                    case StateLocation.OpenReady: // update cost if needed
                    case StateLocation.OpenWaiting:
                        {
                            if( FPUtil.Less( current.Lookup( nextID ).G + edgeCost, current.Lookup( childID ).G ) )
                            {
                                double oldGCost = current.Lookup( childID ).G;
                                current.Lookup( childID ).ParentID = nextID;
                                current.Lookup( childID ).G = current.Lookup( nextID ).G + edgeCost;
                                current.KeyChanged( childID );

                                StateLocation loc2 = opposite.Lookup( _getStateHash( succ ), out int reverseLoc );
                                if( loc2 == StateLocation.OpenReady || loc2 == StateLocation.OpenWaiting )
                                {
                                    if( FPUtil.Less( current.Lookup( nextID ).G + edgeCost + opposite.Lookup( reverseLoc ).G, _currentCost ) )
                                    {
                                        _currentCost = current.Lookup( nextID ).G + edgeCost + opposite.Lookup( reverseLoc ).G;
                                        _middleNode = succ;
                                    }
                                }
                                else if( loc == StateLocation.Closed )
                                {
                                    current.Remove( childID );
                                }
                            }
                        }
                        break;
                    case StateLocation.Unseen:
                        {
                            StateLocation locReverse = opposite.Lookup( _getStateHash( succ ), out int reverseLoc );
                            if( locReverse != StateLocation.Closed )
                            {
                                double newNodeF = current.Lookup( nextID ).G + edgeCost + heuristic( succ, target );
                                if( FPUtil.Less( newNodeF, _currentCost ) )
                                {
                                    if( FPUtil.Less( newNodeF, _queue.GetLowerBound() ) )
                                    {
                                        current.AddOpenNode( succ,
                                            _getStateHash( succ ),
                                            current.Lookup( nextID ).G + edgeCost,
                                            heuristic( succ, target ),
                                            nextID, StateLocation.OpenReady );
                                    }
                                    else
                                    {
                                        current.AddOpenNode( succ,
                                            _getStateHash( succ ),
                                            current.Lookup( nextID ).G + edgeCost,
                                            heuristic( succ, target ),
                                            nextID, StateLocation.OpenWaiting );
                                    }

                                    if( locReverse == StateLocation.OpenReady || locReverse == StateLocation.OpenWaiting )
                                    {
                                        if( FPUtil.Less( current.Lookup( nextID ).G + edgeCost + opposite.Lookup( reverseLoc ).G, _currentCost ) )
                                        {
                                            _currentCost = current.Lookup( nextID ).G + edgeCost + opposite.Lookup( reverseLoc ).G;
                                            _middleNode = succ;
                                        }
                                    }
                                }
                            }
                        }
                        break;
                }
            }
        }
    }
}