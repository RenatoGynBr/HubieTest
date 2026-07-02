/* =====================================================================
   ticketDetailController (REQUESTER) — TODO (candidate area)
   Ticket detail + interaction thread + attachments. The requester can also
   reply (interact) and attach files.
   ===================================================================== */
angular.module('hubieTest').controller('ticketDetailController',
    ['$scope', '$stateParams', 'apiService',
    function ($scope, $stateParams, apiService) {

        $scope.ticketId = parseInt($stateParams.id, 10);
        $scope.ticket = null;
        $scope.interactions = [];
        $scope.attachments = [];
        $scope.newMessage = '';

        $scope.loading = false;
        $scope.error = null;
        $scope.replyLoading = false;
        $scope.replyError = null;

        $scope.load = function () {
            $scope.loading = true;
            $scope.error = null;

            apiService.request('ashx/process/ticket.ashx', 'get', JSON.stringify({ ticketId: $scope.ticketId }))
                .then(function (r) {
                    $scope.ticket = r.data;
                    return apiService.request('ashx/process/ticket.ashx', 'listInteractions', JSON.stringify({ ticketId: $scope.ticketId }));
                })
                .then(function (r) {
                    $scope.interactions = r.data || [];
                    return apiService.request('ashx/process/ticket.ashx', 'listAttachments', JSON.stringify({ ticketId: $scope.ticketId }));
                })
                .then(function (r) {
                    $scope.attachments = r.data || [];
                })
                .catch(function (err) {
                    console.error('Load error', err);
                    $scope.error = (err.data && err.data.error) ? err.data.error : 'Unable to load ticket data.';
                })
                .finally(function () {
                    $scope.loading = false;
                });
        };

        $scope.reply = function () {
            var msg = ($scope.newMessage || '').trim();
            if (!msg) return;

            $scope.replyLoading = true;
            $scope.replyError = null;

            apiService.request('ashx/process/ticket.ashx', 'addInteraction', JSON.stringify({ ticketId: $scope.ticketId, message: msg }))
                .then(function () {
                    $scope.newMessage = '';
                    // reload interactions and ticket header
                    return apiService.request('ashx/process/ticket.ashx', 'listInteractions', JSON.stringify({ ticketId: $scope.ticketId }));
                })
                .then(function (r) {
                    $scope.interactions = r.data || [];
                    return apiService.request('ashx/process/ticket.ashx', 'get', JSON.stringify({ ticketId: $scope.ticketId }));
                })
                .then(function (r) {
                    $scope.ticket = r.data;
                })
                .catch(function (err) {
                    console.error('Reply error', err);
                    $scope.replyError = (err.data && err.data.error) ? err.data.error : 'Unable to send reply.';
                })
                .finally(function () {
                    $scope.replyLoading = false;
                });
        };
        $scope.load();
    }
]);
