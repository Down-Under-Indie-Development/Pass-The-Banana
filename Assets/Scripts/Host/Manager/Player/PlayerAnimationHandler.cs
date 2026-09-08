using UnityEngine;
using Utility;

public class PlayerAnimationHandler : Singleton<PlayerAnimationHandler>
{

    #region Animation Handler
    public void SetAnimator<T>(Animator animator, string paramName, T value)
    {
        if (value is bool b)
            animator.SetBool(paramName, b);
        else if (value is int i)
            animator.SetInteger(paramName, i);
        else if (value is float f)
            animator.SetFloat(paramName, f);
    }
    #endregion
}