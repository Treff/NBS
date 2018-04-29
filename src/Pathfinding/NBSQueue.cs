using System;

namespace Pathfinding
{
    public class NBSQueue<TState>
    {
        public readonly BDOpenClosed<TState> ForwardQueue;
        public readonly BDOpenClosed<TState> BackwardQueue;
        private readonly double _epsilon;
        private double _lowerBound;

        public NBSQueue( double epsilon )
        {
            ForwardQueue = new BDOpenClosed<TState>();
            BackwardQueue = new BDOpenClosed<TState>();
            _epsilon = epsilon;
        }

        public bool GetNextPair( out int nextForward, out int nextBackward )
        {
            // move items with f < lowerBound to ready
            nextForward = nextBackward = -1;
            while( ForwardQueue.OpenWaitingSize() != 0 && FPUtil.Less(
                     ForwardQueue.PeekAt( StateLocation.OpenWaiting ).G + ForwardQueue.PeekAt( StateLocation.OpenWaiting ).H,
                     _lowerBound ) )
            {
                ForwardQueue.PutToReady();
            }
            while( BackwardQueue.OpenWaitingSize() != 0 && FPUtil.Less(
                     BackwardQueue.PeekAt( StateLocation.OpenWaiting ).G + BackwardQueue.PeekAt( StateLocation.OpenWaiting ).H,
                     _lowerBound ) )
            {
                BackwardQueue.PutToReady();
            }

            while( true )
            {
                if( ForwardQueue.OpenSize() == 0 )
                {
                    return false;
                }
                if( BackwardQueue.OpenSize() == 0 )
                {
                    return false;
                }
                if( ( ForwardQueue.OpenReadySize() != 0 ) && ( BackwardQueue.OpenReadySize() != 0 ) &&
                    ( !FPUtil.Greater(
                        ForwardQueue.PeekAt( StateLocation.OpenReady ).G + BackwardQueue.PeekAt( StateLocation.OpenReady ).G + _epsilon,
                        _lowerBound ) ) )
                {
                    nextForward = ForwardQueue.Peek( StateLocation.OpenReady );
                    nextBackward = BackwardQueue.Peek( StateLocation.OpenReady );
                    return true;
                }
                bool changed = false;

                if( BackwardQueue.OpenWaitingSize() != 0 )
                {
                    BDOpenClosedData<TState> i4 = BackwardQueue.PeekAt( StateLocation.OpenWaiting );
                    if( !FPUtil.Greater( i4.G + i4.H, _lowerBound ) )
                    {
                        changed = true;
                        BackwardQueue.PutToReady();
                    }
                }
                if( ForwardQueue.OpenWaitingSize() != 0 )
                {
                    BDOpenClosedData<TState> i3 = ForwardQueue.PeekAt( StateLocation.OpenWaiting );
                    if( !FPUtil.Greater( i3.G + i3.H, _lowerBound ) )
                    {
                        changed = true;
                        ForwardQueue.PutToReady();
                    }
                }
                if( !changed )
                {
                    _lowerBound = double.MaxValue;
                    if( ForwardQueue.OpenWaitingSize() != 0 )
                    {
                        BDOpenClosedData<TState> i5 = ForwardQueue.PeekAt( StateLocation.OpenWaiting );
                        _lowerBound = Math.Min( _lowerBound, i5.G + i5.H );
                    }
                    if( BackwardQueue.OpenWaitingSize() != 0 )
                    {
                        BDOpenClosedData<TState> i6 = BackwardQueue.PeekAt( StateLocation.OpenWaiting );
                        _lowerBound = Math.Min( _lowerBound, i6.G + i6.H );
                    }
                    if( ( ForwardQueue.OpenReadySize() != 0 ) && ( BackwardQueue.OpenReadySize() != 0 ) )
                        _lowerBound = Math.Min( _lowerBound,
                            ForwardQueue.PeekAt( StateLocation.OpenReady ).G + BackwardQueue.PeekAt( StateLocation.OpenReady ).G + _epsilon );
                }
            }
        }

        public void Reset()
        {
            _lowerBound = 0;
            ForwardQueue.Reset();
            BackwardQueue.Reset();
        }

        public double GetLowerBound()
        {
            return _lowerBound;
        }

        public bool TerminateOnG()
        {
            if( ForwardQueue.OpenReadySize() > 0 && BackwardQueue.OpenReadySize() > 0 )
                return FPUtil.Equal( _lowerBound, ForwardQueue.PeekAt( StateLocation.OpenReady ).G +
                     BackwardQueue.PeekAt( StateLocation.OpenReady ).G + _epsilon );
            return false;
        }
    }
}