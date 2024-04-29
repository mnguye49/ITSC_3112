using System;
using System.Collections;
using System.Collections.Generic;
//using System.Diagnostics.Eventing.Reader;
using UnityEngine;

public enum BattleState { Start, PlayerAction, PlayerMove, EnemyMove, Busy}

public class BattleSystem : MonoBehaviour
{
// fields
    [SerializeField] BattleUnit playerUnit;
    [SerializeField] BattleHud playerHud;

    [SerializeField] BattleUnit enemyUnit;
    [SerializeField] BattleHud enemyHud;

    [SerializeField] BattleDialogBox dialogBox;

    public event Action<bool> OnBattleOver;             // lets us know if player won or lost battle

    BattleState state;
    int currentAction;
    int currentMove;

// Initiate the battle
    public void StartBattle()
    {
        StartCoroutine(SetupBattle());
    }

    // IEnumerator gives methods the ability to become a Coroutine
    public IEnumerator SetupBattle()
    {
        playerUnit.Setup();
        playerHud.SetData(playerUnit.Pokemon);

        enemyUnit.Setup();
        enemyHud.SetData(enemyUnit.Pokemon);

        dialogBox.SetMoveNames(playerUnit.Pokemon.Moves);
        // yield return waits for the code within the line to finish then moves onto the next line...
        // in this line it will wait for the Dialog Box to finish typing before moving on
        yield return StartCoroutine(dialogBox.TypeDialog($"A wild {enemyUnit.Pokemon.Base.Name} appeared."));
        
        // wait for 1 second
        yield return new WaitForSeconds(1f);

        // call the player action method
        PlayerAction();
    }

    void PlayerAction()
    {
        state = BattleState.PlayerAction;
        StartCoroutine(dialogBox.TypeDialog("Choose an action"));
        dialogBox.EnableActionSelector(true);
    }

// set the BattleState to the player's turn
    void PlayerMove()
    {
        state = BattleState.PlayerMove;
        dialogBox.EnableActionSelector(false);
        dialogBox.EnableDialogText(false);
        dialogBox.EnableMoveSelector(true);
    }

    // select an attack and do the damage to opposing pokemon
    IEnumerator PerformPlayerMove()
    {
        state = BattleState.Busy;
        // Decrease the move PP by 1 after it is used
        playerUnit.Pokemon.Moves[currentMove].PP -= 1;
        var move = playerUnit.Pokemon.Moves[currentMove];
        
        // Display on HUD that the player's Pokemon used the move
        yield return dialogBox.TypeDialog($"{playerUnit.Pokemon.Base.Name} used {move.Base.Name}");

       // Play attack animation
        playerUnit.PlayAttackAnimation();
        yield return new WaitForSeconds(1f);

        // Play the opponent's hit animation
        enemyUnit.PlayHitAnimation();

       // Have opponent take damage and update their HP accordingly
        var damageDetails = enemyUnit.Pokemon.TakeDamage(move, playerUnit.Pokemon);
        yield return enemyHud.UpdateHP();
        yield return ShowDamageDetails(damageDetails);

        // If the opponent has fainted, display a message saying so
        if (damageDetails.Fainted)
        {
            yield return dialogBox.TypeDialog($"{enemyUnit.Pokemon.Base.Name} Fainted");
            enemyUnit.PlayFaintAnimation();

            yield return new WaitForSeconds(2f);
            OnBattleOver(true);

        }
        // if the opponent has not fainted yet, start their turn
        else
        {
            StartCoroutine(EnemyMove());
        }
    }

// for the opponet's turn
    IEnumerator EnemyMove()
    {
    // set state to the opponent's turn
        state = BattleState.EnemyMove;
        // randomly select a move from their moveset
        var move = enemyUnit.Pokemon.GetRandomMove();

        yield return dialogBox.TypeDialog($"{enemyUnit.Pokemon.Base.Name} used {move.Base.Name}");

        enemyUnit.PlayAttackAnimation();
        yield return new WaitForSeconds(1f);

        playerUnit.PlayHitAnimation();

        var damageDetails = playerUnit.Pokemon.TakeDamage(move, enemyUnit.Pokemon);
        yield return playerHud.UpdateHP();
        yield return ShowDamageDetails(damageDetails);

// if the Pokemon has fainted, display a message stating so
        if (damageDetails.Fainted)
        {
            yield return dialogBox.TypeDialog($"{playerUnit.Pokemon.Base.Name} Fainted");
            playerUnit.PlayFaintAnimation();

            yield return new WaitForSeconds(2f);
            OnBattleOver(false);
        }
        else
        {
            PlayerAction();
        }
    }

    IEnumerator ShowDamageDetails(DamageDetails damageDetails)
    {
        if (damageDetails.Critical > 1f)
        {
            yield return dialogBox.TypeDialog("A critical hit!");
            yield return new WaitForSeconds(1f);
        }

        if (damageDetails.TypeEffectiveness > 1f)
        {
            yield return dialogBox.TypeDialog("It's Super Effective!");
            yield return new WaitForSeconds(1f);
        }
        else
        {
            yield return dialogBox.TypeDialog("It's not very effective!");
            yield return new WaitForSeconds(1f);
        }
    }

    public void HandleUpdate()
    {
        if (state == BattleState.PlayerAction)
        {
            HandleActionSelection();
        }    

        else if (state == BattleState.PlayerMove)
        {
            HandleMoveSelection();
        }
    }

    // increments currentAction variable up and down based off key presses. 
    // our list is oriented top -> down so 0 is the top most option and 1 is the bottom most
    void HandleActionSelection()
    {
    // Press down key to move down the menu
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            if(currentAction < 1)
            {
                ++currentAction;
            }
        }
        // press up key to move up the menu
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            if (currentAction > 0)
            {
                --currentAction;
            }
        }

        dialogBox.UpdateActionSelection(currentAction);
// press Z key to select
// the player can either run or fight
        if (Input.GetKeyDown(KeyCode.Z))
        {
            if (currentAction == 0) 
            {
                // fight
                PlayerMove();

            }
            else if (currentAction == 1) 
            {
                // run
            }
        }
    }

    void HandleMoveSelection()
    {
        // down arrow to move down the move menu
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            if (currentMove < playerUnit.Pokemon.Moves.Count - 2)
            {
                currentMove += 2;
            }
        }

        // up arrow to move up the move menu
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            if (currentMove > 1)
            {
                currentMove -= 2;
            }
        }

        // left arrow to go to the move on the left
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (currentMove > 0)
            {
                --currentMove;
            }
        }

        // right arrow to go to the move on the right
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (currentMove < playerUnit.Pokemon.Moves.Count - 1)
            {
                ++currentMove;
            }
        }

        // display on the HUD the move being selected
        dialogBox.UpdateMoveSelection(currentMove, playerUnit.Pokemon.Moves[currentMove]);

// press the Z key to choose the move
        if (Input.GetKeyDown(KeyCode.Z))
        {
            dialogBox.EnableMoveSelector(false);
            dialogBox.EnableDialogText(true);
            StartCoroutine(PerformPlayerMove());
        }
    }
}
