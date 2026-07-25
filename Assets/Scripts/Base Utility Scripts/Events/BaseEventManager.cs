using Utility;
using UnityEngine;

namespace Events
{
    public class BaseEventManager : Singleton<BaseEventManager>
    {
        public delegate void NoArgs();
        public delegate void OneArg<T1>(T1 t1);
        public delegate void TwoArgs<T1, T2>(T1 t1, T2 t2);
        public delegate void ThreeArgs<T1, T2, T3>(T1 t1, T2 t2, T3 t3);
        public delegate void FourArgs<T1, T2, T3, T4>(T1 t1, T2 t2, T3 t3, T4 t4);

    }
}