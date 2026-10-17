using UnityEngine;

public class TicketSpawner : MonoBehaviour
{
    public GameObject birdMovieTicket;
    public GameObject lastOfYouTicket;

    public void ShowBirdTicket()
    {
        birdMovieTicket.SetActive(true);
        lastOfYouTicket.SetActive(false);
    }

    public void ShowLastOfYou()
    {
        lastOfYouTicket.SetActive(true);
        birdMovieTicket.SetActive(false);
    }
}
