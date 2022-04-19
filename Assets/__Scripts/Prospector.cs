using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;  //Будет использовать позже
using UnityEngine.UI; //Будет позже 

public class Prospector : MonoBehaviour {

	static public Prospector S;

    [Header("Set in Inspector")]
    public TextAsset deckXML;
    public TextAsset layoutXML;
    public float xOffset = 3;
    public float yOffset = -2.5f;
    public Vector3 layoutCenter;
    public Vector2 fsPosMid = new Vector2(0.5f, 0.90f);
    public Vector2 fsPosRun = new Vector2(0.5f, 0.75f);
    public Vector2 fsPosMid2 = new Vector2(0.4f, 1.0f);
    public Vector2 fsPosEnd = new Vector2(0.5f, 0.95f);
    public float reloadDelay = 2f; //Задержка между раундами 2 секунды
    public Text gameOverText, roundResultText, highScoreText;

    [Header("Set Dinamically")]
    public Deck deck;
    public Layout layout;
    public List<CardProspector> drawPile;
    public Transform layoutAnchor;
    public CardProspector target;
    public List<CardProspector> tableau;
    public List<CardProspector> discardPile;
    public FloatingScore fsRun;

     void Awake()
    {
        S = this; //Подготовка объекта одиночки Prospector
        SetUpUITexts();
    }

    void SetUpUITexts()
    {
        //Настроить объект HighScore
        GameObject go = GameObject.Find("HighScore");
        if (go != null)
        {
            highScoreText = go.GetComponent<Text>();
        }
        int highScore = ScoreManager.HIGH_SCORE;
        string hScore = "High Score: " + Utils.AddCommasToNumber(highScore);
        go.GetComponent<Text>().text = hScore;

        //Настроить надписи, отображаемые в конце раунда
        go = GameObject.Find("GameOver");
        if (go != null)
        {
            gameOverText = go.GetComponent<Text>();
        }

        go = GameObject.Find("RoundResult");
        if(go != null)
        {
            roundResultText = go.GetComponent<Text>();
        }

        //Скрыть надписи
        ShowResultsUI(false);
    }

    void ShowResultsUI(bool show)
    {
        gameOverText.gameObject.SetActive(show);
        roundResultText.gameObject.SetActive(show);
    }

    void Start()
    {
        Scoreboard.S.score = ScoreManager.SCORE;

        deck = GetComponent<Deck>(); //Получить компонент Deck
        deck.InitDeck(deckXML.text); //Передать ему deckXML
        Deck.Shuffle(ref deck.cards); //Перемешать колоду передав ее по ссылке


        //Card c;
        //for(int cNum=0; cNum<deck.cards.Count; cNum++)
        //{
        //    c = deck.cards[cNum];
        //    c.transform.localPosition = new Vector3((cNum % 13) * 3, cNum / 13 * 4, 0);
        //}
        layout = GetComponent<Layout>(); //Получить компонент Layout
        layout.ReadLayout(layoutXML.text); //Передать ему содержимое LayoutXML
        drawPile = ConvertListCardsToListCardProspectors(deck.cards);
        LayoutGame();
    }

    List<CardProspector> ConvertListCardsToListCardProspectors(List<Card> lCD)
    {
        List<CardProspector> lCP = new List<CardProspector>();
        CardProspector tCP;
        foreach(Card tCD in lCD)
        {
            tCP = tCD as CardProspector;
            lCP.Add(tCP);
        }
        return (lCP);
    }

    //Функция Draw снимает одну карту с вершины drawPile и возвращает ее 
    CardProspector Draw()
    {
        CardProspector cd = drawPile[0]; //Снять 0-ю карту CardProspector
        drawPile.RemoveAt(0); //Удалить из List<> drawPile
        return (cd); //И вернуть ее
    }

    //LayoutGame() Размещает карты в начальной раскладке - "шахте"
    void LayoutGame()
    {
        //Создать пустой игрововй объект, который будет служить центром раскладки
        if(layoutAnchor == null)
        {
            GameObject tGO = new GameObject("_LayoutAnchor");
            //^Создать пустой игровой объект с именем _LayoutAnchor в иерарахии
            layoutAnchor = tGO.transform; //Получить его компонент Transform
            layoutAnchor.transform.position = layoutCenter; //Поместить в центр
        }

        CardProspector cp;
        //Разложить карты
        foreach (SlotDef tSD in layout.slotDefs)
        {
            //^Выполнить обход всех определений SlotDef в layout.slotDefs
            cp = Draw(); //Выбрать первую карту (сверху) из стопки drawPile
            cp.faceUp = tSD.faceUp; //Установить ее признак faceUp в соответствии с определением в SlotDef
            cp.transform.parent = layoutAnchor; //Назначить layoutAnchor ее родителем
            //Эта операция заменит предыдущего родителя: deck.deckAnchor, который после запуска игры отображался в иерархии с именем _Deck
            cp.transform.localPosition = new Vector3(layout.multiplier.x * tSD.x, layout.multiplier.y * tSD.y, -tSD.layerID);
            //^Установить localPosition карты в соответствии с определением в SlotDef
            cp.layoutID = tSD.id;
            cp.slotDef = tSD;
            //Карты CardProspector в основной раскладке имеют состояние CardState.tableau
            cp.state = eCardState.tableau;
            cp.SetSortingLayerName(tSD.layerName); //Назначить слой сортировки
            tableau.Add(cp); //ДОбавить карту в список tableau
        }

        //Настроить списки карт, мешающих перевернуть данную
        foreach(CardProspector tCP in tableau)
        {
            foreach(int hid in tCP.slotDef.hiddenBy)
            {
                cp = FindCardByLayoutID(hid);
                tCP.hiddenBy.Add(cp);
            }
        }
        //Выбрать начальную целевую карту
        MoveToTarget(Draw());
        //Разложить стопку свободных карт
        UpdateDrawPile();
    }

    //Преобразует номер слота layoutID в экземпляр CardProspector с этим номером
    CardProspector FindCardByLayoutID(int layoutID)
    {
        foreach(CardProspector tCP in tableau)
        {
            //Поиск по всем картам в списке tableau
            if(tCP.layoutID == layoutID)
            {
                //Если номер карты совпадает с искомым, вернуть ее
                return (tCP);
            }
        }
        //Если ничего не найдено, вернуть null
        return (null);
    }

    //Поворачивает карты в основной раскладке лицевой стороной вверх или вниз
    void SetTableauFaces()
    {
        foreach(CardProspector cd in tableau)
        {
            bool faceUp = true; //Предположить что карта должна быть повернута лицевой стороной вверх
            foreach(CardProspector cover in cd.hiddenBy)
            {
                //Если любая из карт, перекрывающих текущую, присутствует в основной раскладке
                if(cover.state == eCardState.tableau)
                {
                    faceUp = false; //Повернуть лицевой стороной вниз
                }
            }
            cd.faceUp = faceUp; //Повернуть карту так или иначе
        }
    }

    //Перемещает целевую карту в стопку сброшенных карт
    void MoveToDiscard(CardProspector cd)
    {
        //Установить состояние карты как discard(сброшена)
        cd.state = eCardState.discard;
        discardPile.Add(cd); //добавить ее в список discardPile
        cd.transform.parent = layoutAnchor; //обновить значение transform.parent

        //Переместить эту карту в позицию стопки сброшенных карт
        cd.transform.localPosition = new Vector3(layout.multiplier.x * layout.discardPile.x, layout.multiplier.y * layout.discardPile.y, -layout.discardPile.layerID + 0.5f);
        cd.faceUp = true;
        //Поместить поверх стопки для сортировки по глубине
        cd.SetSortingLayerName(layout.discardPile.layerName);
        cd.SetSortOrder(-100 + discardPile.Count);
    }

    //Делает карту cd новой целевой картой
    void MoveToTarget(CardProspector cd)
    {
        //Если целевая карта существует, переместить ее в стопку сброшенных карт
        if (target != null) MoveToDiscard(target);
        target = cd; //cd - новая целевая карта
        cd.state = eCardState.target;
        cd.transform.parent = layoutAnchor;

        //Переместить на место для целевой карты
        cd.transform.localPosition = new Vector3(layout.multiplier.x * layout.discardPile.x, layout.multiplier.y * layout.discardPile.y, -layout.discardPile.layerID);
        cd.faceUp = true;
        //Настроить сортировку по глубине
        cd.SetSortingLayerName(layout.discardPile.layerName);
        cd.SetSortOrder(0);
    }

    //Раскладывает стопку свободных карт, чтобы было видно, сколько карт осталось
    void UpdateDrawPile()
    {
        CardProspector cd;
        //Выполнить обход всех карт в drawPile
        for(int i =0; i<drawPile.Count; i++)
        {
            cd = drawPile[i];
            cd.transform.parent = layoutAnchor;

            //Расположить с учетом смещения layout.drawPile.stagger
            Vector2 dpStagger = layout.drawPile.stagger;
            cd.transform.localPosition = new Vector3(layout.multiplier.x * (layout.drawPile.x + i * dpStagger.x), layout.multiplier.y * (layout.drawPile.y + i * dpStagger.y), -layout.drawPile.layerID + 0.1f * i);
            cd.faceUp = false;
            cd.state = eCardState.drawpile;
            //Настроить сортировку по глубине
            cd.SetSortingLayerName(layout.drawPile.layerName);
            cd.SetSortOrder(-10 * i);
        }
    }

    //CardClicked вызывается в ответ на щелчок на любой карте
    public void CardClicked(CardProspector cd)
    {
        //Реакция определяется состоянием карты
        switch (cd.state)
        {
            case eCardState.target:
                //Щелчок на целевой карте игнорится
                break;
            case eCardState.drawpile:
                //Щелчок на любой карте в стопке свободных карт приводит к смене целевой карты
                MoveToDiscard(target); //Переместить целевую карту в discardPile
                MoveToTarget(Draw()); //Верхнюю карту переместить в целевую
                UpdateDrawPile(); //Повторно разложить стопку свободных карт
                ScoreManager.EVENT(eScoreEvent.draw);
                FloatingScoreHandler(eScoreEvent.draw);
                break;
            case eCardState.tableau:
                //Для карты в основной раскладке проверяет возможность ее перемещения на место целевой
                bool validMatch = true;
                if (!cd.faceUp)
                {
                    //Карта, повернутая лицевой сторонй вниз, не может быть перемещена
                    validMatch = false;
                }
                if(!AdjacentRank(cd, target))
                {
                    //Если правило старшенства не соблюдается, карта не может быть перемещена
                    validMatch = false;
                }
                if (!validMatch) return; //Выйти, если карту нельзя переместить
                tableau.Remove(cd); //Удалить из списка tableau
                MoveToTarget(cd); //Сделать эту карту целевой
                SetTableauFaces(); //Повернуть карты в основной раскладке лицевой стороной вверх или вниз
                ScoreManager.EVENT(eScoreEvent.mine);
                FloatingScoreHandler(eScoreEvent.mine);
                break;
        }

        //Проверить завершение игры
        CheckForGameOver();
    }

    //Проверяет завершение игры
    void CheckForGameOver()
    {
        //Если основная раскладка опустела, игра завершена
        if(tableau.Count == 0)
        {
            //Вызывать GameOver() c признаком победы
            GameOver(true);
            return;
        }
        //Если еще есть свободные карты, игра не завершилась
        if (drawPile.Count > 0)
        {
            return;
        }
        //Проверить наличие допустимых ходов
        foreach(CardProspector cd in tableau)
        {
            if(AdjacentRank(cd, target))
            {
                //Если есть допустимый ход, игра не завершилась
                return;
            }
        }
        //Так как допустимых ходов нет, игра завершилась
        //Вызвать GameOver с признаком проигрыша
        GameOver(false);
    }

    //Вызывается, когда игра завершилась.
    void GameOver(bool won)
    {
        int score = ScoreManager.SCORE;
        if (fsRun != null) score += fsRun.score;
        if (won)
        {
            gameOverText.text = "Round Over";
            roundResultText.text = "You won this round!\nRound Score: " + score;
            ShowResultsUI(true);
            //print("Game Over. You won! :)");
            ScoreManager.EVENT(eScoreEvent.gameWin);
            FloatingScoreHandler(eScoreEvent.gameWin);
        }
        else
        {
            gameOverText.text = "Game Over";
            if (ScoreManager.HIGH_SCORE <= score)
            {
                string str = "You got the high score!\nHigh score: " + score;
                roundResultText.text = str;
            }
            else
            {
                roundResultText.text = "Your final score was: " + score;
            }
            ShowResultsUI(true);
            //print("Game Over. You Lost. :(");
            ScoreManager.EVENT(eScoreEvent.gameLoss);
            FloatingScoreHandler(eScoreEvent.gameLoss);
        }
        //Перезагрузить сцену и сбросить игру в исходное состояние
        //SceneManager.LoadScene("__Prospector_Scene_0");

        //Перезагрузить сцену через reloadDelay секунд
        //Это позволит числку с очками долететь до места назначения
        Invoke("ReloadLevel", reloadDelay);
    }

    void ReloadLevel()
    {
        //Перезагрузить сцену и сбросить игру в исходное состояние
        SceneManager.LoadScene("__Prospector_Scene_0");
    }

    //Возвращает true, если две карты соответствуют правилу старшенства
    //с учетом циклического переноса старшенства между тузом и королем
    public bool AdjacentRank (CardProspector c0, CardProspector c1)
    {
        //Если любая из карт повернута лицевой стороной вниз, правило старшенства не соблюдается
        if (!c0.faceUp || !c1.faceUp) return (false);
        //Если достоинства карт отличаются на 1, правило старшенства соблюдается
        if(Mathf.Abs(c0.rank - c1.rank) == 1)
        {
            return (true);
        }
        //Если одна карта - туз, а другая - король, правило старшенства соблюдается
        if (c0.rank == 1 && c1.rank == 13) return (true);
        if (c0.rank == 13 && c1.rank == 1) return (true);
        //Иначе вернуть false
        return (false);
    }

    //Обрабатывает движение FloatingScore
    void FloatingScoreHandler(eScoreEvent evt)
    {
        List<Vector2> fsPts;
        switch (evt)
        {
            //В случае победы, проигрыша и завершения хода выполняются одни и те же действия
            case eScoreEvent.draw: //Выбор свободной карты
            case eScoreEvent.gameWin: //Победа в раунде
            case eScoreEvent.gameLoss: //Проигрыш в раунде
                //Добавить fsRun в ScoreBoard
                if(fsRun != null)
                {
                    //Создать точки для кривой Безье
                    fsPts = new List<Vector2>();
                    fsPts.Add(fsPosRun);
                    fsPts.Add(fsPosMid2);
                    fsPts.Add(fsPosEnd);
                    fsRun.reportFinishTo = Scoreboard.S.gameObject;
                    fsRun.Init(fsPts, 0, 1);
                    //Также скорректировать fontSize
                    fsRun.fontSizes = new List<float>(new float[] { 28, 36, 4 });
                    fsRun = null;
                }
                break;

            case eScoreEvent.mine:  //Удаление карты из основной раскладки
                //Создать FloatingScore для отображения этого количества очков
                FloatingScore fs;
                //Переместить из позиции указателя мыши mousePosition в fsPosRun
                Vector2 p0 = Input.mousePosition;
                p0.x /= Screen.width;
                p0.y /= Screen.height;
                fsPts = new List<Vector2>();
                fsPts.Add(p0);
                fsPts.Add(fsPosMid);
                fsPts.Add(fsPosRun);
                fs = Scoreboard.S.CreateFloatingScore(ScoreManager.CHAIN, fsPts);
                fs.fontSizes = new List<float>(new float[] { 4, 50, 28 });
                if(fsRun == null)
                {
                    fsRun = fs;
                    fsRun.reportFinishTo = null;
                }
                else
                {
                    fs.reportFinishTo = fsRun.gameObject;
                }
                break;
        }
    }
}
