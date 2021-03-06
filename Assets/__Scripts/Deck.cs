using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Deck : MonoBehaviour {

    [Header("Set in Inspector")]
    public bool startFaceUp = false;
    //Масти
    public Sprite suitClub;
    public Sprite suitDiamond;
    public Sprite suitHeart;
    public Sprite suitSpade;

    public Sprite[] faceSprites;
    public Sprite[] rankSprites;

    public Sprite cardBack;
    public Sprite cardBackGold;
    public Sprite cardFront;
    public Sprite cardFrontGold;

    //Шаблоны
    public GameObject prefabCard;
    public GameObject prefabSprite;

    [Header("Set Dinamically")]
    public PT_XMLReader xmlr;
    public List<string> cardNames;
    public List<Card> cards;
    public List<Decorator> decorators;
    public List<CardDefinition> cardDefs;
    public Transform deckAnchor;
    public Dictionary<string, Sprite> dictSuits;

    //InitDeck вызывается экземпляром Prospector, когда будет готов
    public void InitDeck(string deckXMLText)
    {
        //Создать точку привязки для всех ИО Card в иерархии
        if(GameObject.Find("_Deck")== null)
        {
            GameObject anchorGO = new GameObject("_Deck");
            deckAnchor = anchorGO.transform;
        }

        //Инициализировать словарь со спрайтами значков мастей
        dictSuits = new Dictionary<string, Sprite>()
        {
            {"C", suitClub },
            {"H", suitHeart },
            {"D", suitDiamond },
            {"S", suitSpade }
        };

        ReadDeck(deckXMLText); //Эта срань существовала в предыдущей версии сценария

        MakeCards();
    }

    //ReadDeck читает указанный XML файл и создает массив экземпляров CardDefinition
    public void ReadDeck(string deckXMLText)
    {
        xmlr = new PT_XMLReader(); //Создать новый экземпляр PT_XMLReader
        xmlr.Parse(deckXMLText); //Использовать его для чтения deckXML

        //Вывод проверочной строки, чтобы показать, как использовать xmlr.
        //За дополнительной инфой по чтению XML пиздуйте в раздел "Полезные идеи".
        string s = "xml[0] decorator[0] ";
        s += "type=" + xmlr.xml["xml"][0]["decorator"][0].att("type");
        s += " x=" + xmlr.xml["xml"][0]["decorator"][0].att("x");
        s += " y=" + xmlr.xml["xml"][0]["decorator"][0].att("y");
        s += " scale=" + xmlr.xml["xml"][0]["decorator"][0].att("scale");
        //print(s);

        //Прочитать элементы <decorator> для всех карт
        decorators = new List<Decorator>(); //инициализировать список экземпляров Decorator
        //Извлечь список PT_XMLHaxhList всех элементов <decorator> из XML-файла
        PT_XMLHashList xDecos = xmlr.xml["xml"][0]["decorator"];
        Decorator deco;
        for(int i=0; i<xDecos.Count; i++)
        {
            //для каждого элемента <decorator> в XML
            deco = new Decorator(); //Create instance Decorator
            //Copy attributes from <decorator> to Decorator
            deco.type = xDecos[i].att("type");
            //deco.flip получит значение true, если аттрибут flip содержит текст 1
            deco.flip = (xDecos[i].att("flip") == "1");
            //Получить значения float из строковых аттрибутов
            deco.scale = float.Parse(xDecos[i].att("scale"));
            //Vector3 loc инициализируется как [0, 0, 0], поэтому нам остается только изменить его
            deco.loc.x = float.Parse(xDecos[i].att("x"));
            deco.loc.y = float.Parse(xDecos[i].att("y"));
            deco.loc.z = float.Parse(xDecos[i].att("z"));
            //Добавить deco в список decorators
            decorators.Add(deco);

        }

        //Прочитать координаты для значков, определяющих достоинство карты
        cardDefs = new List<CardDefinition>(); //Инициализировать список карт
        //Извлечь список PT_XMLHashList всех элементов <card> из XML-файла
        PT_XMLHashList xCardDefs = xmlr.xml["xml"][0]["card"];
        for (int i = 0; i < xCardDefs.Count; i++)
        {
            //Для каждого элемента <card> создать экземпляр CardDefinition
            CardDefinition cDef = new CardDefinition();
            //Получить значения атрибута и добавить их в cDef
            cDef.rank = int.Parse(xCardDefs[i].att("rank"));
            //Извлечь список PT_XMLHashList всех элементов <pip> внутри этого элемента <card>
            PT_XMLHashList xPips = xCardDefs[i]["pip"];
            if (xPips != null)
            {
                for (int j = 0; j < xPips.Count; j++)
                {
                    //Обойти все элементы <pip>
                    deco = new Decorator();
                    //Элементы <pip> в <card> обрабатываются классом Decorator
                    deco.type = "pip";
                    deco.flip = (xPips[j].att("flip") == "1");
                    deco.loc.x = float.Parse(xPips[j].att("x"));
                    deco.loc.y = float.Parse(xPips[j].att("y"));
                    deco.loc.z = float.Parse(xPips[j].att("z"));
                    if (xPips[j].HasAtt("scale"))
                    {
                        deco.scale = float.Parse(xPips[j].att("scale"));
                    }
                    cDef.pips.Add(deco);
                }
            }
            //Карты с картинками (Валет, Дама, Король) имеют аттрибут face
            if (xCardDefs[i].HasAtt("face"))
            {
                cDef.face = xCardDefs[i].att("face");
            }
            cardDefs.Add(cDef);
        }
    }

    //Получает CardDefinition на основе значения достоинства (от 1 до 14 - от туза до короля)
    public CardDefinition GetCardDefinitionByRank(int rnk)
    {
        //Поиск во всех определениях CardDefinition
        foreach (CardDefinition cd in cardDefs)
        {
            //Если достоинство совпадает, вернуть его определение
            if(cd.rank == rnk)
            {
                return (cd);
            }
        }
        return (null);
    }

    //Создает ИО карт
    public void MakeCards()
    {
        //cardNames будет содержать имена сконструированных карт
        //Каждая масть имеет 14 значений достоинства
        //Например для треф (Clubs) от С1 до С14
        cardNames = new List<string>();
        string[] letters = new string[] { "C", "H", "D", "S" };
        foreach (string s in letters)
        {
            for(int i =0; i<13; i++)
            {
                cardNames.Add(s + (i + 1));
            }
        }
        //Создать список со всеми картами
        cards = new List<Card>();

        //Обойти все только что созданные имена карт
        for(int i=0; i<cardNames.Count; i++)
        {
            //Создать карту и добавить ее в колоду
            cards.Add(MakeCard(i));
        }
    }

    private Card MakeCard(int cNum)
    {
        //Создать новый ИО с картой
        GameObject cgo = Instantiate(prefabCard) as GameObject;
        //Настроить transform.parent новой карты в соответствии с точкой привязки
        cgo.transform.parent = deckAnchor;
        Card card = cgo.GetComponent<Card>(); //получить компонент Card

        //Эта строка выкладывает карты в аккуратный ряд
        cgo.transform.localPosition = new Vector3((cNum % 13) * 3, cNum / 13 * 4, 0);

        //Настроить основные параметры карты
        card.name = cardNames[cNum];
        card.suit = card.name[0].ToString();
        card.rank = int.Parse(card.name.Substring(1));
        if(card.suit == "D" || card.suit == "H")
        {
            card.colS = "Red";
            card.color = Color.red;
        }
        //Получить CardDefinition для этой карты
        card.def = GetCardDefinitionByRank(card.rank);

        AddDecorators(card);
        AddPips(card);
        AddFace(card);
        AddBack(card);

        return (card);
    }

    //Следующие скрытые переменные используются вспомогательными методами
    private Sprite _tSp = null;
    private GameObject _tGO = null;
    private SpriteRenderer _tSR = null;

    private void AddDecorators(Card card)
    {
        //Добавить оформление
        foreach (Decorator deco in decorators)
        {
            if(deco.type == "suit")
            {
                //Создать экземпляр ИО спрайта
                _tGO = Instantiate(prefabSprite) as GameObject;
                //Получить ссылку на компонент SpriteRenderer
                _tSR = _tGO.GetComponent<SpriteRenderer>();
                //Установить спрайт масти
                _tSR.sprite = dictSuits[card.suit];
            }
            else
            {
                _tGO = Instantiate(prefabSprite) as GameObject;
                _tSR = _tGO.GetComponent<SpriteRenderer>();
                //Получить спрайт для отображения достоинства
                _tSp = rankSprites[card.rank];
                //Установить спрайт достоинства в SpriteRenderer
                _tSR.sprite = _tSp;
                //Установить цвет соответствующей масти
                _tSR.color = card.color;
            }
            //Поместить спрайты над картой
            _tSR.sortingOrder = 1;
            //Сделать спрайт дочерним по отношению к карте
            _tGO.transform.SetParent(card.transform);
            //Установить localPosition как определено в DeckXML
            _tGO.transform.localPosition = deco.loc;
            //Перевернуть значок если необходимо
            if(deco.flip)
            {
                //Эйлеров поворот на 180 градусов относительно оси Z-axis
                _tGO.transform.rotation = Quaternion.Euler(0, 0, 180);

            }
            //Установить масштаб, чтобы уменьшить размер спрайта
            if(deco.scale != 1)
            {
                _tGO.transform.localScale = Vector3.one * deco.scale;
            }
            //Дать имя этому ИО для наглядности
            _tGO.name = deco.type;
            //Добавить этот ИО с оформлением в список card.decoGOs
            card.decoGOs.Add(_tGO);
        }
    }

    private void AddPips(Card card)
    {
        //Дла каждого значка в определении...
        foreach(Decorator pip in card.def.pips)
        {
            //...Создать ИО спрайта
            _tGO = Instantiate(prefabSprite) as GameObject;
            //Назначить родителем ИО карты
            _tGO.transform.SetParent(card.transform);
            //Установить localPosition как определено в XML-файле
            _tGO.transform.localPosition = pip.loc;
            //Перевернуть, если необходимо
            if (pip.flip)
            {
                _tGO.transform.rotation = Quaternion.Euler(0, 0, 180);
            }
            //Масштабировать, если необходимо (только для туза)
            if(pip.scale != 1)
            {
                _tGO.transform.localScale = Vector3.one * pip.scale;
            }
            //Дать имя игровому объекту
            _tGO.name = "pip";
            //Получить ссылку на компонент SpriteRenderer
            _tSR = _tGO.GetComponent<SpriteRenderer>();
            //Установить спрайт масти
            _tSR.sprite = dictSuits[card.suit];
            //Установить sortingOrder, чтобы значок отображался над Card_Front
            _tSR.sortingOrder = 1;
            //Добавить этот ИО в список значков
            card.pipGOs.Add(_tGO);
        }
    }

    private void AddFace(Card card)
    {
        if(card.def.face == "")
        {
            return; //Выйти, если эта карта без картинки

        }
        _tGO = Instantiate(prefabSprite) as GameObject;
        _tSR = _tGO.GetComponent<SpriteRenderer>();
        //Сгенерировать имя и передать его в GetFace()
        _tSp = GetFace(card.def.face + card.suit);
        _tSR.sprite = _tSp; //Установить этот спрайт в _tSR
        _tSR.sortingOrder = 1;
        _tGO.transform.SetParent(card.transform);
        _tGO.transform.localPosition = Vector3.zero;
        _tGO.name = "face";
    }

    //Находит спрайт с картинкой для карты
    private Sprite GetFace(string faceS)
    {
        foreach(Sprite _tSP in faceSprites)
        {
            //Если найден спрайт с требуемым именем...
            if (_tSP.name == faceS)
            {
                //Вернуть его
                return (_tSP);
            }
        }
        //Если ничего не найдено, вернуть null
        return (null);
    }

    private void AddBack(Card card)
    {
        //Добавить рубашку
        //Card_Back будет покрывать все остальное на карте
        _tGO = Instantiate(prefabSprite) as GameObject;
        _tSR = _tGO.GetComponent<SpriteRenderer>();
        _tSR.sprite = cardBack;
        _tGO.transform.SetParent(card.transform);
        _tGO.transform.localPosition = Vector3.zero;
        //Большее значение sortingOrder чем у других спрайтов
        _tSR.sortingOrder = 2;
        _tGO.name = "back";
        card.back = _tGO;

        //По умолчанию картинкой вверх
        card.faceUp = startFaceUp; //Использовать свойство faceUp карты

    }

    //Перемешивает карты в Deck.cards
    static public void Shuffle(ref List<Card> oCards)
    {
        //Создать временный список хранящий карты в перемешанном порядке
        List<Card> tCards = new List<Card>();

        int ndx; //Будет хранить индекс перемещаемой карты
        tCards = new List<Card>(); //Инициализировать временный список
        //Повторять пока не будут перемешаны все карты в исходном списке
        while (oCards.Count > 0)
        {
            //Выбрать случайный индекс карты
            ndx = Random.Range(0, oCards.Count);
            //Добавить эту карту во временный список
            tCards.Add(oCards[ndx]);
            //и удалить эту карту из исходного списка
            oCards.RemoveAt(ndx);
        }
        //Заменить исходный список временным
        oCards = tCards;
        //Так как oCards - это параметр-ссылка(ref), оригинальный аргумент переданный в метод тоже изменится
    }
}
