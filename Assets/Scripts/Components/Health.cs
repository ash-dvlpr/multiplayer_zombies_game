using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Health : Resource {
    // ==================== Configuration ====================
    public override ResourceType ResType { get => ResourceType.Plentiful; }

    // ====================== Unity Code ======================
    protected override void Awake() {
        base.Awake();
    }

    // ===================== Custom Code =====================
    public void Heal(int amount) {
        Amount += Math.Max(0, amount);
    }

    public void Damage(int amount) {
        Amount -= Math.Max(0, amount);
    }

    // ================== Outside Facing API ==================
    protected override void TriggerOnChange() {
        base.TriggerOnChange();

        if (Amount.Equals(0)) {
            onDeath?.Invoke(this);
        }
    }

    private event Action<Health> onDeath;
    public event Action<Health> OnDeath {
        add    { lock(this) { onDeath += value; } }
        remove { lock(this) { onDeath -= value; } }
    }
}
