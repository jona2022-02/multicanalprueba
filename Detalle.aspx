<%@ Page Title="Detalle" Language="VB" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Detalle.aspx.vb" Inherits="multicanalprueba.Detalle" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <main>
        <h2>Empresa seleccionada</h2>
        <div class="card p-4">
            <asp:Image ID="imgLogo" runat="server" Width="80" Height="80" CssClass="mb-3" />
            <h3><asp:Literal ID="litNombre" runat="server" /></h3>
            <p class="text-muted">Id: <asp:Literal ID="litId" runat="server" /></p>
            <p class="text-muted">Canal: <asp:Literal ID="litCanal" runat="server" /></p>
            <asp:HyperLink ID="lnkVolver" runat="server" CssClass="btn btn-secondary" Text="Volver al listado" />
        </div>
    </main>
</asp:Content>
