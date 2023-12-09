using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Internal;

/// <summary>
/// Generic abstract resource component.
/// </summary>
public abstract class AResource : NetworkBehaviour {
    public enum ResourceType : uint {
        /// <summary>
        /// <see cref="Amount">Amount</see> will start off at it's <see cref="Max">Max</see> value.
        /// </summary>
        Plentiful = 0,
        /// <summary>
        /// <see cref="Amount">Amount</see> will start off at 0.
        /// </summary>
        Scarse = 1,
    }

    // ==================== Configuration ====================
    [field: Header("Configuration")]
    [field: SerializeField, Min(1)] public int Max { get; private set; }

    /// <value>
    /// Determines the behaviour of the <see cref="Reset">Reset()</see> method.
    /// </value>
    public abstract ResourceType ResType { get; }

    // ====================== Variables ======================
    [field: SerializeField, ShowOnly][SyncVar(OnChange = nameof(TriggerOnChange))] private int _amount;
    public int Amount {
        get => _amount;
        protected set { _amount = Math.Clamp(value, 0, Max); }
    }

    // ====================== Unity Code ======================
#if UNITY_EDITOR
    void OnValidate() {
        Max = Math.Max(1, Max);

        if (!Application.isPlaying) Reset();
        else Amount = Amount;
    }
#endif

    protected void Reset() {
        switch (ResType) {
            case ResourceType.Scarse:
                Amount = 0; break;
            case ResourceType.Plentiful:
                Amount = Max; break;
            default:
                throw new NotImplementedException($"'Resource.Reset()': Missing implementation for enum variant: '{ResType}'");
        }
    }

    protected virtual void Awake() {
        Reset();
    }

    // ================== Outside Facing API ==================
    protected virtual void TriggerOnChange(int prev, int next, bool asServer) { 
        onChange?.Invoke(this);
    }

    private event Action<AResource> onChange;
    public event Action<AResource> OnChange {
        add    { lock(this) { onChange += value; } }
        remove { lock(this) { onChange -= value; } }
    }
}