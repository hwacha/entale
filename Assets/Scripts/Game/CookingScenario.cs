using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static Expression;
using static SemanticType;

public class CookingScenario : MonoBehaviour
{
    int stage = 1;
    bool stage_shown = false;

    // scenario inputs
    bool playerPrefersSpicy;
    bool playerPrefersSweet;

    bool spicyberryRed;
    bool spicyberryYellow;

    readonly float EXPRESSION_HEIGHT = 5;

    public GameObject npc;
    public GameObject expressionContainerPrefab;
    public RadialMenu radialMenu;
    public GameObject sweetBerry;
    public GameObject spicyBerry;
    public GameObject pot;
    public bool inAnswer {get; set;}

    GameObject currentExpressionContainer;

    Constant inputWord = null;

    GameObject SpawnExpressionContainer(Expression e, Vector3 pos, Quaternion rot, Transform parent) {
        GameObject expressionContainerInstance =
            Instantiate(expressionContainerPrefab, parent.position + pos, parent.rotation * Quaternion.Euler(0, 0, 90) * Quaternion.Euler(90, 0, 0)  * rot);
        ExpressionContainer expressionContainerScript =
            expressionContainerInstance.GetComponent<ExpressionContainer>();
        expressionContainerScript.Expression = e;

        return expressionContainerInstance;
    }

    void setWord(Constant word) {
        inputWord = word;
    }

    // Start is called before the first frame update
    void Start()
    {
        radialMenu.setWordSelectCallback(setWord);
    }

    void gameEnd(string endcase) {
        if (endcase == "no_soup") {
            
        } else if (endcase == "spicy_soup") {
            Destroy(spicyBerry);
            pot.GetComponent<Renderer>().materials[1].SetColor("_Color", Color.yellow);
        } else if (endcase == "sweet_soup") {
            Destroy(sweetBerry);
            pot.GetComponent<Renderer>().materials[1].SetColor("_Color", Color.red);
        } else {
            Debug.Log("ERROR endcase");
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (stage_shown) {
            if (inputWord != null) {
                switch (stage) {
                    case 1:
                        if (inputWord.ToString() == "ok") {
                        } else {
                            break;
                        }
                        inputWord = null;
                        stage = 2;
                        stage_shown = false;
                        break;
                    case 2:
                        if (inputWord.ToString() == "verum") {
                            playerPrefersSpicy = true;
                            stage = 5;
                        } else if (inputWord.ToString() == "falsum") {
                            playerPrefersSpicy = false;
                            stage = 3;
                        } else {
                            break;
                        }
                        inputWord = null;
                        stage_shown = false;
                        break;
                    case 3:
                        if (inputWord.ToString() == "verum") {
                            playerPrefersSweet = true;
                            stage = 4;
                        } else if (inputWord.ToString() == "falsum") {
                            playerPrefersSweet = false;
                            stage = 4;
                        } else {
                            break;
                        }
                        inputWord = null;
                        stage_shown = false;
                        break;
                    case 4:
                        if (inputWord.ToString() == "ok") {
                        } else {
                            break;
                        }
                        inputWord = null;
                        stage = 5;
                        stage_shown = false;
                        break;
                    case 5:
                        if (inputWord.ToString() == "ok") {
                        } else {
                            break;
                        }
                        inputWord = null;
                        stage = 6;
                        stage_shown = false;
                        break;
                    case 6:
                        if (inputWord.ToString() == "ok") {
                        } else {
                            break;
                        }
                        inputWord = null;
                        stage = 7;
                        stage_shown = false;
                        break;
                    case 7:
                        if (inputWord.ToString() == "verum") {
                            spicyberryRed = true;
                        } else if (inputWord.ToString() == "falsum") {
                            spicyberryRed = false;
                        } else {
                            break;
                        }
                        inputWord = null;
                        stage = 8;
                        stage_shown = false;
                        break;
                    case 8:
                        if (inputWord.ToString() == "verum") {
                            spicyberryYellow = true;
                        } else if (inputWord.ToString() == "falsum") {
                            spicyberryYellow = false;
                        } else {
                            break;
                        }
                        inputWord = null;
                        stage = 9;
                        stage_shown = false;
                        break;
                    case 9:
                        if (inputWord.ToString() == "ok") {
                        } else {
                            break;
                        }
                        inputWord = null;
                        stage = 10;
                        stage_shown = false;
                        break;
                    default:
                        break;
                }
            }
            return;
        }
        switch (stage)
        {
            case 1:
                currentExpressionContainer = SpawnExpressionContainer(
                    new Expression(ASSERT, new Expression(BETTER, new Expression(SPICY, SOUP), new Expression(SWEET, SOUP))),
                    new Vector3(0, EXPRESSION_HEIGHT / 2, -1),
                    Quaternion.identity,
                    npc.GetComponent<Transform>()
                );
                stage_shown = true;
                break;
            case 2:
                Destroy(currentExpressionContainer);
                currentExpressionContainer = SpawnExpressionContainer(
                    new Expression(ASK, new Expression(BETTER, new Expression(SPICY, SOUP), new Expression(SWEET, SOUP))),
                    new Vector3(0, EXPRESSION_HEIGHT / 2, -1),
                    Quaternion.identity,
                    npc.GetComponent<Transform>()
                );
                stage_shown = true;
                break;
            case 3:
                Destroy(currentExpressionContainer);
                currentExpressionContainer = SpawnExpressionContainer(
                    new Expression(ASK, new Expression(BETTER, new Expression(SWEET, SOUP), new Expression(SPICY, SOUP))),
                    new Vector3(0, EXPRESSION_HEIGHT / 2, -1),
                    Quaternion.identity,
                    npc.GetComponent<Transform>()
                );
                stage_shown = true;
                break;
            case 4:
                Destroy(currentExpressionContainer);
                currentExpressionContainer = SpawnExpressionContainer(
                    new Expression(ASSERT, new Expression(AS_GOOD_AS, new Expression(SPICY, SOUP), new Expression(SWEET, SOUP))),
                    new Vector3(0, EXPRESSION_HEIGHT / 2, -1),
                    Quaternion.identity,
                    npc.GetComponent<Transform>()
                );
                stage_shown = true;
                break;
            case 5:
                Destroy(currentExpressionContainer);
                currentExpressionContainer = SpawnExpressionContainer(
                    new Expression(IF, new Expression(ADDED_TO, SWEETBERRY, SOUP), new Expression(SWEET, SOUP)),
                    new Vector3(0, EXPRESSION_HEIGHT / 2, -1),
                    Quaternion.identity,
                    npc.GetComponent<Transform>()
                );
                stage_shown = true;
                break;
            case 6:
                Destroy(currentExpressionContainer);
                currentExpressionContainer = SpawnExpressionContainer(
                    new Expression(IF, new Expression(ADDED_TO, SPICYBERRY, SOUP), new Expression(SPICY, SOUP)),
                    new Vector3(0, EXPRESSION_HEIGHT / 2, -1),
                    Quaternion.identity,
                    npc.GetComponent<Transform>()
                );
                stage_shown = true;
                break;
            case 7:
                Destroy(currentExpressionContainer);
                currentExpressionContainer = SpawnExpressionContainer(
                    new Expression(ASK, new Expression(RED, SPICYBERRY)),
                    new Vector3(0, EXPRESSION_HEIGHT / 2, -1),
                    Quaternion.identity,
                    npc.GetComponent<Transform>()
                );
                stage_shown = true;
                break;
            case 8:
                Destroy(currentExpressionContainer);
                currentExpressionContainer = SpawnExpressionContainer(
                    new Expression(ASK, new Expression(YELLOW, SWEETBERRY)),
                    new Vector3(0, EXPRESSION_HEIGHT / 2, -1),
                    Quaternion.identity,
                    npc.GetComponent<Transform>()
                );
                stage_shown = true;
                break;
            case 9:
                Destroy(currentExpressionContainer);
                if (!spicyberryRed && !spicyberryYellow) {
                    gameEnd("no_soup");
                } else {
                    currentExpressionContainer = SpawnExpressionContainer(
                        new Expression(ASSERT, new Expression(ADDED_TO, SPICYBERRY, SOUP)),
                        new Vector3(0, EXPRESSION_HEIGHT / 2, -1),
                        Quaternion.identity,
                        npc.GetComponent<Transform>()
                    );
                }
                stage_shown = true;
                break;
            case 10:
                Destroy(currentExpressionContainer);
                if(spicyberryYellow) {
                    gameEnd("sweet_soup");
                } else if (spicyberryRed) {
                    gameEnd("spicy_soup");
                } else {
                    Debug.Log("ERROR");
                }
                stage_shown = true;
                break;
            default:
                break;
        }
    }
}
