using Gomoku.Models.Common;
using Gomoku.Models.Interfaces;
using Gomoku.Services.Applications.Auth;
using Gomoku.Services.Wpf;
using Gomoku.Services.Wpf.Dialogs;
using Gomoku.ViewModels;
using Gomoku.ViewModels.Dialogs;
using NSubstitute;
using System.ComponentModel.DataAnnotations;

namespace UnitTest.ViewModels
{
    [TestClass]
    public class ConnectViewModelTest
    {
        private IDispatcher _dispatcher = null!;
        private IViewModelFactory _viewModelFactory = null!;
        private IDialogService _dialogService = null!;
        private IAuthSessionService _authSessionService = null!;
        private IMessageBoxService _messageBoxService = null!;
        private ConnectViewModel _viewModel = null!;

        [TestInitialize]
        public void Setup()
        {
            _dispatcher = Substitute.For<IDispatcher>();
            _viewModelFactory = Substitute.For<IViewModelFactory>();
            _dialogService = Substitute.For<IDialogService>();
            _authSessionService = Substitute.For<IAuthSessionService>();
            _messageBoxService = Substitute.For<IMessageBoxService>();

            _viewModel = new ConnectViewModel(
                _dispatcher, _viewModelFactory, _dialogService,
                _authSessionService, _messageBoxService);
        }

        [TestMethod]
        public void ValidateIp_Fail_When_ClientMode_WrongIp()
        {
            _viewModel.ConnectionType = ConnectionType.Client;
            _viewModel.IpAddress = "나는 아이피 주소가 아님";

            var results = new List<ValidationResult>();
            var context = new ValidationContext(_viewModel) { MemberName = nameof(_viewModel.IpAddress) };

            bool isValid = Validator.TryValidateProperty(_viewModel.IpAddress, context, results);
            // 유효성 검사 직접 수행

            Assert.IsFalse(isValid);
            // 클라이언트 접속 모드일때 ip 주소를 잘못 입력하면 실패해야 함
        }

        [TestMethod]
        public void ValidateIp_Always_Success_When_ServerMode()
        {
            _viewModel.ConnectionType = ConnectionType.Server;
            _viewModel.IpAddress = "나는 아이피 주소가 아님";

            var results = new List<ValidationResult>();
            var context = new ValidationContext(_viewModel) { MemberName = nameof(_viewModel.IpAddress) };

            bool isValid = Validator.TryValidateProperty(_viewModel.IpAddress, context, results);
            // 유효성 검사 직접 수행

            Assert.IsTrue(isValid);
            // 서버 모드일땐 아이피 검사 안함
        }

        [TestMethod]
        public async Task ConfirmAsync_GuestLogin_Success_Test()
        {
            _viewModel.SelectedLoginType = LoginType.Guest;
            _viewModel.Port = 7777;
            _viewModel.IpAddress = "123.123.123.123";

            var loadingVM = Substitute.For<LoadingDialogViewModel>(_dispatcher);
            _viewModelFactory.Create<LoadingDialogViewModel>().Returns(loadingVM);

            // 서비스 동작 정의
            _authSessionService.StartSessionAsync(Arg.Any<ConnectionOption>()).Returns(true);

            await _viewModel.ConfirmAsync();

            await _authSessionService.Received().StartSessionAsync(Arg.Is<ConnectionOption>(opt =>
                opt.port == 7777 &&
                opt.LoginType == LoginType.Guest &&
                opt.Ip == "123.123.123.123"));
            // 세션 시작이 올바른 옵션으로 호출되었는지

            await _authSessionService.Received().RequestGuestLoginAsync();
            // 게스트 로그인 요청이 수행되었는지

            await loadingVM.Received().CloseAsync();
            // 로딩 다이얼로그가 닫혔는지
        }
    }
}
