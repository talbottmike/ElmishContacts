﻿namespace ElmishContacts

open Models
open Repository
open Style
open Elmish.XamarinForms
open Elmish.XamarinForms.DynamicViews
open Xamarin.Forms

module MainPage =
    type Msg = | ContactsLoaded of Contact list
               | ContactSelected of Contact
               | AboutTapped
               | AddNewContactTapped
               | ShowMapTapped
               | ContactAdded of Contact
               | ContactUpdated of Contact
               | ContactDeleted of Contact

    type ExternalMsg = | NoOp
                       | Select of Contact
                       | About
                       | AddNewContact
                       | ShowMap

    type Model =
        {
            Contacts: Contact list option
        }

    let loadAsyncCmd dbPath = async {
        let! contacts = loadAllContacts dbPath
        return ContactsLoaded contacts
    }

    let findContactIn (groupedContacts: (string * Contact list) list) (gIndex: int, iIndex: int) =
        groupedContacts.[gIndex]
        |> (fun (_, items) -> items.[iIndex])

    let init dbPath () =
        {
            Contacts = None
        }, Cmd.ofAsyncMsg (loadAsyncCmd dbPath)

    let update msg model =
        match msg with
        | ContactsLoaded contacts -> { model with Contacts = Some contacts }, Cmd.none, ExternalMsg.NoOp
        | ContactSelected contact -> model, Cmd.none, (ExternalMsg.Select contact)
        | AboutTapped -> model, Cmd.none, ExternalMsg.About
        | AddNewContactTapped -> model, Cmd.none, ExternalMsg.AddNewContact
        | ShowMapTapped -> model, Cmd.none, ExternalMsg.ShowMap
        | ContactAdded contact ->
            let newContacts = model.Contacts.Value @ [ contact ]
            { model with Contacts = Some newContacts }, Cmd.none, ExternalMsg.NoOp
        | ContactUpdated contact ->
            let newContacts = model.Contacts.Value |> List.map (fun c -> if c.Id = contact.Id then contact else c)
            { model with Contacts = Some newContacts }, Cmd.none, ExternalMsg.NoOp
        | ContactDeleted contact ->
            let newContacts = model.Contacts.Value |> List.filter (fun c -> c <> contact)
            { model with Contacts = Some newContacts }, Cmd.none, ExternalMsg.NoOp

    let view model dispatch =
        dependsOn model.Contacts (fun model mContacts ->
            View.ContentPage(
                title="ElmishContacts",
                toolbarItems=[
                    mkToolbarButton "About" (fun() -> dispatch AboutTapped)
                    mkToolbarButton "Add" (fun() -> dispatch AddNewContactTapped)
                ],
                content=View.StackLayout(
                    children=
                        match mContacts with
                        | None ->
                            [ mkCentralLabel "Loading..." ]
                        | Some [] ->
                            [ mkCentralLabel "No contact" ]
                        | Some contacts ->
                            let groupedContacts =
                                contacts
                                |> List.map (fun c -> (c, c.LastName.[0].ToString().ToUpper()))
                                |> List.sortBy snd
                                |> List.groupBy snd
                                |> List.map (fun (k, l) -> (k, List.map fst l))
                            
                            [
                                View.ListViewGrouped(
                                    verticalOptions=LayoutOptions.FillAndExpand,
                                    rowHeight=60,
                                    showJumpList=(contacts.Length > 10),
                                    itemTapped=(findContactIn groupedContacts >> ContactSelected >> dispatch),
                                    items=
                                        [
                                            for (groupName, items) in groupedContacts do
                                                yield groupName, mkGroupView groupName,
                                                        [
                                                            for contact in items do
                                                                let address = contact.Address.Replace("\n", " ")
                                                                yield mkCachedCellView contact.Picture (contact.FirstName + " " + contact.LastName) address contact.IsFavorite
                                                        ]
                                        ]
                                )
                                View.Button(
                                    text="Show contacts on map",
                                    command=(fun () -> dispatch ShowMapTapped)
                                )
                            ]
                ) 
            )
        )