using UnityEngine;

public class TicketSpawner : MonoBehaviour
{
    public GameObject birdMovieTicket;
    public GameObject lastOfYouTicket;
    public CustomerQueue customerQueue;   // Drag the object with CustomerQueue here

    public void ShowBirdTicket()
    {
        

        birdMovieTicket.SetActive(true);
        lastOfYouTicket.SetActive(false);

        customerQueue.SubmitTicketChoice(0);   // 0 = Bird Movie
    }

    public void ShowLastOfYou()
    {
       

        lastOfYouTicket.SetActive(true);
        birdMovieTicket.SetActive(false);

        customerQueue.SubmitTicketChoice(1);   // 1 = Last of You
    }
}