using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Перечисление, определяющее тип переменной, которая может принимать несколько предопределенных значений
public enum eCardState
{
    drawpile,
    tableau,
    target,
    discard
}

public class CardProspector : Card { //CardProspector должен расширять Card
    [Header("Set Dynamically: CardProspector")]
    //Так используется перечисление eCardState
    public eCardState state = eCardState.drawpile;
    //hiddenBy - список других карт, не позволяющих перевернуть эту лицом вверх
    public List<CardProspector> hiddenBy = new List<CardProspector>();
    //LayoutID определяет для этой карты ряд в раскладке
    public int layoutID;
    //Класс SlotDef хранит информацию из элемента <slot> в LayoutXML
    public SlotDef slotDef;

    //Определяет реакцию карт на щелчок мыши
    public override void OnMouseUpAsButton()
    {
        //Вызвать метод CardClicked объекта-одиночки Prospector
        Prospector.S.CardClicked(this);
        //а также версию этого метода в базовом классе (Card.cs)
        base.OnMouseUpAsButton();
    }
}
