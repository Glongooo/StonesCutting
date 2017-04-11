using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineDraw : MonoBehaviour
{
    GameObject current_obj;                             //пустой объект
    
    private Vector3 clicks;                            //переменныя для координат курсора
    public GameObject line;                            //оригинал объекта линии
    public List<GameObject> lines;                     //массив с объектами линии
    public int indpoint = 0, n=0, m = 0, b = 1;
    private bool onhitplus = false;
    
    public Material[] randMaterial = new Material[6];   //массив с цветами линий
    int random=-1;                                      //рандомно выбираем цвет линии

    void Start()
    {      
        var lineinst = Instantiate(line);                 //локальная переменная которыя содержит клон линии
        lines.Add(lineinst);                               //вставляем клон в массив
        
        lines[n].GetComponent<LineRenderer>().startWidth = 0f;
        lines[n].GetComponent<LineRenderer>().endWidth = 0f; //убираем толщину линии т.к. иначе она отображается в координатах 0,0 по clicks
    }


    void Update()
    {        
        clicks = Camera.main.ScreenToWorldPoint(Input.mousePosition);              //координаты курсора
        lines[n].GetComponent<LineRenderer>().SetPosition(indpoint + 1, clicks); // последняя точка линии всегда находиться в координатах курсора
        //------создает линию между спрайтами и сохраняет её когда отпускаем палец от экрана
        //Общая задача этого блока скриптов: рисовать линию между спрайтами(спавн спрайтов скрипт sp2) пока не убрал палец от экрана между любым количеством точек
        //Подзадачи:
        //1. провел пальцем по N спрайтам между ними создалась линия, отжал палец в пустом месте: линия между N спрайтами сохранилась, часть линии от поледнего спрайта до пальца удалилась. 
        //2. кликнул по 1 прайту и отжал палец в пустом месте(т.е. всего 1 точка линии не считая пальца) линия удалилась без сохранения
        //3. провел пальцем по N спрайтам между ними создалась линия, отжал палец на последнем спрайте(не в пустом месте): линия между N спрайтами сохранилась. --это почти работает
        //4. нажал на 1 спрайте отвел палец в пустое место и снова вернул его на этот же спрайт(но палец не отжимал): ничего не происходит(в масиве 1 точка линии в коорлинатах спрайта вторая за пальцем)
        //5. нажал на 1 спрайт и тут же убрал палец с экрана: ничего не происходит, т.е. 1 точка линии которая записалась в координатах 1 спрайта удаляется, созданный объект линия удаляется.
        //6. закончил рисовать линию и от последней точки предыдущей линии рисуешь новую линию -- у меня не рисует новую линию от последней точки предыдущей линии.
        
        if (Input.GetMouseButtonDown(0))
        {
             RaycastHit2D hit0 = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero, 0f);
            
            if (hit0.transform != null)
            {               
                if (hit0.transform.GetComponent<SpriteRenderer>() &&
                    current_obj!= hit0.transform.gameObject)   //если кликнули по спрайту первый раз
                {                             
                    current_obj = hit0.transform.gameObject;                                                              //получаем координату станции и заносим её в пустой объект
                    if (n <= 6)                                                                                           //если  линий меньше 7 (т.е от 0 до 6)
                    {
                        var lineinst = Instantiate(line);                                                                //объявляем локальную переменную в которую заносим ссылку на клон префаба
                        lines.Add(lineinst);                                                                             //добавляем префаб в массив через ссылку на него.
                                                 
                        lines[n].GetComponent<LineRenderer>().startWidth = 0.2f;                                            //стартовая толщина линии
                        lines[n].GetComponent<LineRenderer>().endWidth = 0.2f;                                             //толщина конца линии.

                        random = random + 1;                                                                              //назначаем адрес материала в массиве
                        lines[n].GetComponent<LineRenderer>().GetComponent<Renderer>().material = randMaterial[random];   //назначаем текущей линии материал.

                        lines[n].GetComponent<LineRenderer>().SetPosition(indpoint, hit0.transform.position);             //передаем координат станции 
                        onhitplus = true;                                                                                //включаем создание следующих точек.
                    }
                    
                }
            }
        }
        
        if (onhitplus == true)                                               //если начали создание линии, т.е. кликнули по первому спрайту
        { 
            RaycastHit2D hitplus = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero, 0f);

            if (hitplus)
            {
                if (hitplus.transform.gameObject.GetComponent<SpriteRenderer>())
                {
                    if (current_obj != hitplus.transform.gameObject)                //и координаты этого спрайта не совпадают с первым спрайтом
                    {
                        indpoint = indpoint + 1;                                    //указываем место в массиве кк +1

                        current_obj = hitplus.transform.gameObject;                 //передаем координаты в пустой объект что бы была возможность узнать кликали мы по этому спрайту или нет

                        lines[n].GetComponent<LineRenderer>().SetPosition(indpoint, hitplus.transform.position);   //записываем координату точки линии
                       
                    }
                }
                
            }
            
        }

        if (Input.GetMouseButtonUp(0))
        {
            RaycastHit2D hitend = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero, 0f);
            
            if (hitend)
            {
                if (hitend.transform.gameObject.GetComponent<SpriteRenderer>())                                 //если отпустили нажатие на спрайте
                {
                    onhitplus = false;                                                                       //выключаем запись промежуточных точек
                    if (n <= 6)                                                                              //если  линий меньше 7 (т.е от 0 до 6)
                    {
                        lines[n].GetComponent<LineRenderer>().SetPosition(indpoint, hitend.transform.position);  //то сохраняем линию в массиве
                                               
                            n = n + 1;                                                                          //указываем что следующую линию надо записывать в новую ячейку массива
                            indpoint = 0;                                                                        //указываем что точки след. линии надо начинать записывать с нулевого элемента
                            lines[n].GetComponent<LineRenderer>().startWidth = 0f; lines[n].GetComponent<LineRenderer>().endWidth = 0f;  //убираем толщину линии т.к. иначе она отображается в координатах 0,0 по clicks


                    }
                }
                
            }
            
        }

    }

}