<%@ Page Title="Empresas" Language="VB" MasterPageFile="~/Site.Master" AutoEventWireup="true" Async="true" CodeBehind="Empresas.aspx.vb" Inherits="multicanalprueba.Empresas" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <main>
        <h2>Empresas de pago</h2>
        <p class="lead">Canal: <asp:Literal ID="litCanal" runat="server" /></p>

        <asp:Panel ID="pnlError" runat="server" CssClass="alert alert-danger" Visible="false" role="alert">
            <asp:Literal ID="litError" runat="server" />
        </asp:Panel>

        <asp:Repeater ID="rptEmpresas" runat="server" OnItemCommand="rptEmpresas_ItemCommand">
            <HeaderTemplate>
                <div class="row">
            </HeaderTemplate>
            <ItemTemplate>
                <div class="col-md-4 mb-3">
                    <div class="card h-100 text-center p-3">
                        <img src='<%# Eval("LogoUrl") %>' alt="" class="mx-auto" style="width:80px;height:80px;object-fit:contain;" />
                        <div class="card-body">
                            <h5 class="card-title"><%# Server.HtmlEncode(Convert.ToString(Eval("Nombre"))) %></h5>
                            <asp:Button ID="btnSeleccionar" runat="server"
                                CssClass="btn btn-primary"
                                Text="Seleccionar"
                                CommandName="Seleccionar"
                                CommandArgument='<%# Eval("Id") %>' />
                        </div>
                    </div>
                </div>
            </ItemTemplate>
            <FooterTemplate>
                </div>
            </FooterTemplate>
        </asp:Repeater>
    </main>
</asp:Content>
