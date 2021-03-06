﻿using UnityEngine;
using System.Collections;
//using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public int m_NumRoundsToWin = 5;        
    public float m_StartDelay = 3f;         
    public float m_EndDelay = 3f;           
    public CameraControl m_CameraControl;   
    public Text m_MessageText;              
    public GameObject m_TankPrefab;         
    public TankManager[] m_Tanks;           


    private int m_RoundNumber; //Numéro du Round courant             
    private WaitForSeconds m_StartWait; //nombre de secondes avant le commencement    
    private WaitForSeconds m_EndWait;  //nombre de secondes après la fin du Round     
    private TankManager m_RoundWinner; //Le tank qui a gagné le Round
    private TankManager m_GameWinner;  // Le tank qui a gagné la partie     


    private void Start()
    {
        m_StartWait = new WaitForSeconds(m_StartDelay); //Instanciation de l'objet WaitForSeconds qui va permettre de créer une pause
        m_EndWait = new WaitForSeconds(m_EndDelay); // jusqu'à l'écoulement du temps en secondes.

        SpawnAllTanks(); //Une fonction qui va permettre d'instancier tous les Tanks du TankManager
        SetCameraTargets();

        StartCoroutine(GameLoop()); //une coroutine est une fonction où on peut placer une pause pour un certain nombre de secondes et pouvoir reprendre
		//par la suite ce qui reste du code à executer. 
    }


    private void SpawnAllTanks()
    {
        for (int i = 0; i < m_Tanks.Length; i++)
        {
            m_Tanks[i].m_Instance =
                Instantiate(m_TankPrefab, m_Tanks[i].m_SpawnPoint.position, m_Tanks[i].m_SpawnPoint.rotation) as GameObject; //Assigner l'attribut 
			//m_Instance du Tank crée à l'instanciation du Tank et lui appliquer les modifications necessaires. 
            m_Tanks[i].m_PlayerNumber = i + 1; //attribuer la valeur du joueur. Ne pas oublier le +1 car i commence à partir de 0.
            m_Tanks[i].Setup(); //La fonction Setup dans TankManager va permettre de gérer les différentes attributions du Tank
        }
    }


    private void SetCameraTargets() //Cette fonction va permettre de transférer de remplir automatiquement l'attribut targets du Script CameraControl.
    {
        Transform[] targets = new Transform[m_Tanks.Length]; //On va créer un Tableau de Transform de taille le nombre des Tanks.

        for (int i = 0; i < targets.Length; i++)
        {
            targets[i] = m_Tanks[i].m_Instance.transform; //Tous les attributs targets vont avoir comme valeurs les transform enregistrés dans les attributs
			//m_Instance du m_Tank. à Noter que m_Instance représente le gameObject du m_Tank
        }

        m_CameraControl.m_Targets = targets; //l'attribut Targets de CameraControl aura comme valeur le tableau des targets enregistrés.
    }


    private IEnumerator GameLoop()
    {
        yield return StartCoroutine(RoundStarting());
        yield return StartCoroutine(RoundPlaying());
        yield return StartCoroutine(RoundEnding());

        if (m_GameWinner != null)
        {
            SceneManager.LoadScene(0);
        }
        else
        {
            StartCoroutine(GameLoop());
        }
    }


    private IEnumerator RoundStarting()
    {
		//Cette fonction va :
		//Réinitialiser tous les Tanks --> Invoquer la fonction Reset dans TankManager
		//Désactiver tous les contrôles de tous les Tanks --> Invoquer la fonction DisableControl dans TankManager
		//Repositionner et redimensionner la caméra
		//Incrémenter le numéro du Round
		//Créer le MessageUI
		ResetAllTanks (); //Réinitialiser tous les Tanks
		DisableTankControl (); //Désactiver tous les contrôles 
		m_CameraControl.SetStartPositionAndSize (); //Repositionner la caméra 
		m_RoundNumber++; //incrémenter le numéro du Round
		m_MessageText.text = "ROUND "+m_RoundNumber; //Afficher le nouveau Round à l'écran.

        yield return m_StartWait;
    }


    private IEnumerator RoundPlaying()
    {
		//Dans cette fonction : 
		//On va activer tous les contrôles de tous les Tanks. --> Invoquer la fonction EnableControl dans TankManager
		//Effacer le Texte affiché
		//Attendre jusqu'à ce qu'il ne reste qu'un seul Tank en jeu.
		EnableTankControl ();
		m_MessageText.text = string.Empty;
		while (!OneTankLeft ()) {
			yield return null;
		}
    }


    private IEnumerator RoundEnding()
    {
		//Dans cette fonction : 
		//Désactiver tous les contrôles de tous les Tanks --> Invoquer la fonction DisableControl dans TankManager
		//Retourner le Tank qui a gagné le Round
		//Tester si on a gagné la partie 
		//Changer le message à afficher suivant le contexte

		DisableTankControl ();
		m_RoundWinner = null; //Il se peut qu'un Round peut finir à une égalité dans le cas ou les deux Tanks sont détruits en même temps.
		m_RoundWinner = GetRoundWinner (); //Si le retour de cette fonction est null alors il y à un match null, sinon le Tank qui est retourné est le gagnant.

		if (m_RoundWinner != null) {
			m_RoundWinner.m_Wins++; //A rappeler que RoundWinner est un attribut de type TankManager qui contient un attribut Wins qui renseigne sur le 
			//nombre de Round gagnés par ce Tank.
		}

		m_GameWinner = GetGameWinner (); //Même logique que pour le RoundWinner

		string message = EndMessage (); // C'est la fonction qui permet de récupérer le message à afficher suivant le contexte.

		m_MessageText.text = message;

        yield return m_EndWait;
    }


    private bool OneTankLeft()
    {
        int numTanksLeft = 0;

        for (int i = 0; i < m_Tanks.Length; i++)
        {
            if (m_Tanks[i].m_Instance.activeSelf)
                numTanksLeft++;
        }

        return numTanksLeft <= 1;
    }


    private TankManager GetRoundWinner()
    {
        for (int i = 0; i < m_Tanks.Length; i++)
        {
            if (m_Tanks[i].m_Instance.activeSelf)
                return m_Tanks[i];
        }

        return null;
    }


    private TankManager GetGameWinner()
    {
        for (int i = 0; i < m_Tanks.Length; i++)
        {
            if (m_Tanks[i].m_Wins == m_NumRoundsToWin)
                return m_Tanks[i];
        }

        return null;
    }


    private string EndMessage()
    {
        string message = "DRAW!"; //On commence par l'hypothèse de départ c'est qu'il n y à pas de gagnant 

        if (m_RoundWinner != null)
            message = m_RoundWinner.m_ColoredPlayerText + " WINS THE ROUND!"; //Si on  récupère un Tank qui a gagné le Round courant on l'affiche avec sa 
			//couleur correspondante.

        message += "\n\n\n\n"; 

        for (int i = 0; i < m_Tanks.Length; i++)
        {
            message += m_Tanks[i].m_ColoredPlayerText + ": " + m_Tanks[i].m_Wins + " WINS\n"; //Afficher tous les stats de tous les Tanks
        }

        if (m_GameWinner != null)
            message = m_GameWinner.m_ColoredPlayerText + " WINS THE GAME!"; //Afficher ce texte lorsqu'on connait celui qui a gagné la partie

        return message;
    }


    private void ResetAllTanks()
    {
        for (int i = 0; i < m_Tanks.Length; i++)
        {
            m_Tanks[i].Reset();
        }
    }


    private void EnableTankControl()
    {
        for (int i = 0; i < m_Tanks.Length; i++)
        {
            m_Tanks[i].EnableControl();
        }
    }


    private void DisableTankControl()
    {
        for (int i = 0; i < m_Tanks.Length; i++)
        {
            m_Tanks[i].DisableControl();
        }
    }
}