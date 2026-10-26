using UnityEngine;

public class TicketSpawner : MonoBehaviour
{
    public GameObject birdMovieTicket;
    public GameObject lastOfYouTicket;
    public CustomerQueue customerQueue;  

    public void ShowBirdTicket()
    {
        

        birdMovieTicket.SetActive(true);
        lastOfYouTicket.SetActive(false);

        customerQueue.SubmitTicketChoice(0);   
    }

    public void ShowLastOfYou()
    {
       

        lastOfYouTicket.SetActive(true);
        birdMovieTicket.SetActive(false);

        customerQueue.SubmitTicketChoice(1);   
    }
}