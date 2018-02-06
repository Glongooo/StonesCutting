using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AbstractCommand {

    public abstract void Do(object context);
    public abstract void Undo(object context);
}
