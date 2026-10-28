<%@ Page Title="Home Page" Language="VB" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.vb" Inherits="multicanalprueba._Default" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <main>
        <section class="row" aria-labelledby="aspnetTitle">
            <h1 id="aspnetTitle">Multicanal</h1>
            <p class="lead">Sitio de pagos compartido por varios canales (evaluación e-Solutions).</p>
            <p>
                <a class="btn btn-primary btn-md" href="<%= ResolveUrl("~/Empresas?canal=AB") %>">Canal AB (con menú) &raquo;</a>
                <a class="btn btn-secondary btn-md" href="<%= ResolveUrl("~/Empresas?canal=CJ") %>">Canal CJ (sin menú) &raquo;</a>
            </p>
        </section>
    </main>
</asp:Content>
