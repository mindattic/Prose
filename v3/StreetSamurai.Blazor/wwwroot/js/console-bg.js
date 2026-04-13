window.consoleBg = (function () {
    'use strict';

    // ── Window title bar labels ─────────────────────────────────────────────
    var TITLES = [
        'proc/8812',  'proc/9144',  'proc/3301',  'proc/7712',  'proc/4492',
        'proc/1187',  'proc/6603',  'proc/2255',  'proc/5519',  'proc/8001',
        'bci_daemon', 'neural_rx',  'cortex_sync','grip_cls',   'bim_proc',
        'rbs_ctrl',   'hkb_mon',    'port_sel',   'slug_feed',  'buck_feed',
        'kern/sched', 'kern/vm',    'kern/ipc',   'kern/net',   'kern/fs',
        'kern/mm',    'kern/irq',   'kern/sig',   'kern/clock', 'kern/audit',
        'sys/netd',   'sys/logd',   'sys/cryptd', 'sys/authd',  'sys/watchd',
        'sys/arbiter','sys/broker', 'sys/relay',  'sys/mirror', 'sys/janitor',
        'net/tx_mon', 'net/rx_mon', 'net/gate',   'net/proxy',  'net/scanner',
        'net/sniffer','net/balancer','net/tunnel', 'net/beacon', 'net/resolver',
        'db/node_idx','db/edge_map','db/query',   'db/cache',   'db/repl',
        'db/compact', 'db/journal', 'db/txn_log', 'db/vacuum',  'db/snapshot',
        'sec/auditor','sec/policy', 'sec/token',  'sec/vault',  'sec/scanner',
        'sec/ids',    'sec/firewall','sec/probe',  'sec/enclave','sec/signer',
        'glmz/relay', 'glmz/gate',  'glmz/node',  'glmz/border','glmz/comms',
        'crest/rbs',  'crest/bim',  'crest/hkb',  'crest/led',  'crest/cal',
        'corp/enforcer','corp/monitor','corp/audit','corp/sync', 'corp/mirror',
        'dist7/node', 'dist12/hub', 'dist4/fab',  'dist9/relay','dist2/comms',
        'arc/compress','arc/index', 'arc/verify', 'arc/extract','arc/delta',
        'jit/compile', 'jit/cache', 'jit/evict',  'jit/patch',  'jit/trace',
    ];

    // ── Console output lines ────────────────────────────────────────────────
    var LINES = [
        // original 28
        '0x4f3a :: dispatch_sync[0x1b] → q',
        'pid:8812 ipc.recv buf=0xffc2 ok',
        'kern: mmap 0x7ffe3200 sz=4096 rw',
        'net.tx 172.16.44.12:5555 1400B',
        'sys.fork → child:9144 inherit_fd',
        'ring3 trap #14 cr2=0x00000008',
        'sched: ctx-swap 8812→9144 Δ=2μs',
        'fs.read /proc/9144/maps 4096B',
        'db.exec idx_scan nodes n=847',
        'alloc: 0x5582c4a0 64B heap ok',
        'lib: dlopen libneural.so.3 ok',
        'audit: cap_check euid=1000 NET',
        'rx:127.0.0.1:44312 frame 0xd4',
        'jit: compile fn@0x5582c4b8 ok',
        'sig: SIGUSR2 pid:8812 → queued',
        'bim: grip[Δ=+0.027] → slug',
        'rbs: rotate 38ms port_beta lock',
        'bci: cortex_rx lat=4ms handshake',
        'net.syn ACK 0x9f3c seq++',
        'vm: page_fault 0x800 recover',
        'cache: evict lru sz=256K ok',
        'ipc.gate:0x1b [enc payload fwd]',
        'sec: token_verify uid=0 ok',
        'crypt: aes256 blk=0x44 done',
        'tsk: worker:12 idle→run lat=1ms',
        'buf: flush 0x3c00 sz=8192B ok',
        'ev: POLLIN fd=7 consumed',
        'arc: compress ratio=2.4 ok',
        // 100 new entries
        'bci: motor_cortex signal lat=3ms',
        'bci: grip_tension[L]=0.41 stable',
        'bci: intent_cls → precision ok',
        'bci: calibrate epoch=7 Δ=0.003',
        'bci: pair sig 0xBF44 accepted',
        'bci: extensor_detect 12ms lock',
        'bci: sweep_cls → buck confirmed',
        'bci: neural_key 0xA3F7 valid',
        'bci: contact_ring[3] Δ=+0.014',
        'bci: 4hr_cal acc=97.3% commit',
        'rbs: disc idle → alpha port',
        'rbs: beta_lock acquired 38ms',
        'rbs: detent spring load=2.1N',
        'rbs: motor_cmd enc[0x22] ok',
        'rbs: yellow_led transit 38ms',
        'rbs: red_led port_alpha active',
        'rbs: manual_override lever→buck',
        'hkb: piston stroke 8mm recoil',
        'hkb: impulse 8ms→120ms spread',
        'hkb: perceived_kick -40% ok',
        'comp: port1 gas_vent upward ok',
        'comp: muzzle_flip correction ok',
        'port_alpha: round_count=2 ok',
        'port_beta: slug_seated fwd ok',
        'port_beta: brass_cap ejector ok',
        'led: pin_tactile protrude slug',
        'net.tx 10.44.7.3:8443 tls1.3',
        'net.rx 192.168.44.12 ack=0x3c',
        'net: retransmit seq=0x9f3d #2',
        'net: route 10.0.0.0/8 via gw4',
        'net: dns glmz.relay.7 → ok',
        'net: keepalive dist12 ok 44ms',
        'net: tls_handshake → resumed',
        'net: cipher AES-256-GCM ok',
        'net: drop 172.31.0.0 blacklist',
        'net: nat 10.44.7.3→203.0.113.9',
        'glmz: border_node[7] ping 12ms',
        'glmz: district12 relay online',
        'glmz: gate_check corp_key ok',
        'glmz: freelancer_id 0x3301 seen',
        'glmz: comm_node district4 ok',
        'glmz: enforcement_ping no reply',
        'glmz: dark_node 10.x seen 1x',
        'glmz: relay_hop 7→12 lat=18ms',
        'corp: sync_pulse ack 0x44 ok',
        'corp: audit_log appended row 8k',
        'corp: policy_check PASS uid=0',
        'corp: rekey_interval 3600s ok',
        'corp: enclave_attest sig valid',
        'corp: mirror_sync lag=44ms ok',
        'db: edge_insert n=3 commit ok',
        'db: node_update id=0x3301 ok',
        'db: idx_rebuild edges done',
        'db: page_split btree 0x4f ok',
        'db: wal_checkpoint frames=2048',
        'db: vacuum reclaim 12 pages',
        'db: query_plan cost=84 hash-join',
        'db: lock shared tbl:nodes ok',
        'db: txn_begin iso=read-committed',
        'db: txn_commit lsn=0x3af7 ok',
        'mem: brk 0x5582c800 +4096 ok',
        'mem: munmap 0x7f3a0000 ok',
        'mem: huge_page 2M mapped ok',
        'mem: oom_score pid:8812 adj=-8',
        'mem: slab reclaim 64 objects',
        'mem: rss_cur=148M rss_peak=201M',
        'mem: swap_in 4 pages 2ms',
        'mem: numa_balance migrate ok',
        'fs: open /var/log/bci.log rw',
        'fs: fsync /var/db/nodes.db ok',
        'fs: inode 8191 ref++ ok',
        'fs: dentry_cache hit 0x3301',
        'fs: dirty_pages flush 32 ok',
        'fs: ext4 blk_alloc 0x3af grp2',
        'fs: journal commit seq=0x1b ok',
        'crypt: chacha20 stream init ok',
        'crypt: hmac-sha256 verify ok',
        'crypt: ecdh p256 derive ok',
        'crypt: rng reseed RDRAND ok',
        'crypt: x25519 keyex done ok',
        'crypt: kdf pbkdf2 iter=100k ok',
        'sec: iptables ACCEPT dst:8443',
        'sec: uid=1000 cap drop NET_RAW',
        'sec: seccomp allow:read,write',
        'sec: ptrace denied pid:9144',
        'sec: selinux enforce domain ok',
        'sec: cert_verify depth=2 ok',
        'sec: crl_check revoked=false',
        'sec: vault_unseal shard 2/3 ok',
        'sched: load avg 0.82 0.74 0.61',
        'sched: preempt lat p99=88μs',
        'sched: cgroup cpu.shares=512',
        'sched: rr timeslice 5ms reset',
        'irq: softirq NET_RX 14 fired',
        'irq: tasklet bci_rx queued',
        'irq: affinity cpu2 set ok',
        'pci: dev 0:1b.0 dma_map ok',
        'usb: dev 3-1.2 xfer 512B ok',
        'arc: lz4 ratio=3.1 blk=64K',
        'arc: delta_encode base=0x3a ok',
        'arc: integrity sha1 match ok',
        'jit: trace loop@0x5582 hot',
        'jit: inline depth=3 ok',
        'jit: deopt guard miss patch',
        'ipc: shm_attach key=0x3301 ok',
        'ipc: mq_send qid=4 sz=64 ok',
        'ipc: sem_wait set=0 val→0',
        'bci: motor_rx lat=2ms ok',
        'bci: cortex_tx pkt=1 sz=48',
        'bci: grip_cls intent=sweep',
        'bci: calibrate ring=0 ok',
        'bci: neural_key verify ok',
        'bci: bim_proc pid=9144 up',
        'bci: contact_ring 3 active',
        'bci: sweep_score=0.92 ok',
        'bci: precision_score=0.87',
        'bci: epoch 4412 complete',
        'bci: delta_band 13Hz ok',
        'bci: theta_band 7Hz ok',
        'bci: alpha suppress ok',
        'bci: beta_burst detected',
        'bci: gamma spike flt ok',
        'bci: baseline update ok',
        'bci: noise_floor 0.04 ok',
        'bci: ring0 impedance ok',
        'bci: ring1 impedance ok',
        'bci: ring2 impedance ok',
        'bci: ring3 impedance ok',
        'bci: ring4 contact lost',
        'bci: ring4 reacquire ok',
        'bci: calibrate epoch=88',
        'bci: key_derive pbkdf2 ok',
        'bci: hmac verify sig ok',
        'bci: neural_rx buf=2048',
        'bci: cortex_sync 4.1ms',
        'bci: intent=precision ok',
        'bci: intent=idle set',
        'bci: lockout clear ok',
        'bci: operator 0x3301 ok',
        'bci: operator 0x7712 ok',
        'bci: operator 0x4492 auth',
        'bci: handshake accepted',
        'bci: grip_cls conf=0.95',
        'bci: grip_cls conf=0.78',
        'bci: motor signal 48Hz',
        'bci: adc ring=0 val=2048',
        'bci: adc ring=1 val=2051',
        'bci: adc ring=2 val=2039',
        'bci: adc ring=3 val=2044',
        'bci: feature_vec norm ok',
        'bci: classifier v3 load',
        'bci: model_weights hash ok',
        'bci: poll_interval 20ms',
        'bci: slot_queue depth=3',
        'bci: slot_queue drain ok',
        'bci: buffer_flush ok',
        'rbs: disc rotate α→β ok',
        'rbs: disc rotate β→α ok',
        'rbs: port_sel alpha set',
        'rbs: port_sel beta set',
        'rbs: led red active',
        'rbs: led blue active',
        'rbs: led yellow blink',
        'rbs: led green steady',
        'rbs: pin extend ok',
        'rbs: pin retract ok',
        'rbs: slug_feed ch=alpha ok',
        'rbs: slug_feed ch=beta ok',
        'rbs: buck_feed ch=alpha ok',
        'rbs: disc pos=0 locked',
        'rbs: disc pos=1 locked',
        'rbs: rotation 180ms ok',
        'rbs: rotation timeout!',
        'rbs: rotation retry ok',
        'rbs: thermal 38C ok',
        'rbs: thermal 47C warn',
        'rbs: round_count=12',
        'rbs: round_count=7',
        'rbs: empty detect ch=a',
        'rbs: feed_pressure ok',
        'rbs: port_lock engage',
        'rbs: port_lock release',
        'rbs: crest_sig v2.3 ok',
        'rbs: cal_ref 0x3A ok',
        'rbs: bim_cmd recv ok',
        'rbs: override manual set',
        'rbs: override clear ok',
        'hkb: piston extend 3mm',
        'hkb: piston retract ok',
        'hkb: buffer impulse 4.1N',
        'hkb: buffer impulse 6.2N',
        'hkb: buffer impulse 2.8N',
        'hkb: correction +1.2ms',
        'hkb: correction -0.8ms',
        'hkb: recoil absorb ok',
        'hkb: hydraulic psi=220',
        'hkb: hydraulic psi=198',
        'hkb: hydraulic low warn',
        'hkb: fluid level ok',
        'hkb: fluid level low!',
        'hkb: spring tension ok',
        'hkb: cycle complete ok',
        'hkb: cycle_count=4412',
        'hkb: wear_score=0.12',
        'hkb: wear_score=0.87!',
        'hkb: service_due flag',
        'hkb: thermal 42C ok',
        'net.tx 10.44.7.3:8443 1200B',
        'net.tx 172.16.0.1:443 640B',
        'net.rx 10.44.7.3:8443 880B',
        'net.rx 172.16.44.12:5555 400B',
        'net.tx glmz-relay:9000 512B',
        'net.rx glmz-gate:4433 288B',
        'net.tx dist7.node:8080 960B',
        'net.rx dist4.hub:7070 720B',
        'net.tx dist12.relay:6060 384B',
        'net.rx dist9.node:5050 1024B',
        'net.tx enf-node-44:443 256B',
        'net.rx enf-node-44:443 128B',
        'net.tx dark-node:9999 192B',
        'net.tls handshake ok 1.3',
        'net.tls cert verify ok',
        'net.tls sni glmz.relay ok',
        'net.dns resolve relay ok',
        'net.dns resolve failed!',
        'net.dns ttl=300 cached',
        'net.gate check pass ok',
        'net.gate check fail 403',
        'net.gate retry ok',
        'net.proxy CONNECT ok',
        'net.proxy tunnel up',
        'net.proxy tunnel drop',
        'net.scan port 8443 open',
        'net.scan port 443 open',
        'net.scan port 9000 open',
        'net.beacon recv district7',
        'net.beacon recv district4',
        'net.beacon enf active!',
        'net.rst inject detect',
        'net.conn reset peer',
        'net.tcp retry x3 ok',
        'net.tcp backoff 200ms',
        'net.tcp window=65535',
        'net.udp drop rate=0.02',
        'net.route via 10.44.0.1',
        'net.route metric=100 ok',
        'net.route failover ok',
        'net.bridge dist4→dist7',
        'net.tunnel wg up ok',
        'net.tunnel wg drop',
        'net.tunnel wg rekey ok',
        'kern: alloc 8192 ok',
        'kern: alloc 16384 ok',
        'kern: alloc fail ENOMEM',
        'kern: free 0x7ffe3200 ok',
        'kern: mmap sz=8192 rw',
        'kern: mmap sz=65536 rx',
        'kern: munmap ok',
        'kern: fault #14 prot r',
        'kern: fault #14 prot w',
        'kern: fault handled ok',
        'kern: signal 11 pid=9144',
        'kern: signal 9 pid=3301',
        'kern: signal 15 ok',
        'kern: sched preempt ok',
        'kern: sched migrate cpu2',
        'kern: sched wakeup pid=8812',
        'kern: irq 44 handled ok',
        'kern: irq 12 deferred',
        'kern: irq storm cpu0!',
        'kern: vm swapout 4p',
        'kern: vm swapin 2p',
        'kern: vm oom cand pid=3301',
        'kern: vm reclaim 8M ok',
        'kern: rcu_sched stall warn',
        'kern: softirq NET_RX',
        'kern: softirq TASKLET',
        'kern: hrtimer fire ok',
        'kern: clocksource tsc ok',
        'kern: audit pid=8812 ok',
        'kern: audit write denied',
        'kern: ptrace attach ok',
        'kern: seccomp filter ok',
        'kern: seccomp deny open',
        'kern: cgroup mem limit hit',
        'kern: cgroup cpuset ok',
        'kern: namespaces pid ok',
        'kern: namespaces net ok',
        'kern: landlock rule ok',
        'kern: ebpf prog load ok',
        'kern: ebpf map update ok',
        'kern: ebpf verifier pass',
        'kern: kprobes attach ok',
        'kern: perf sample cpu0',
        'kern: numa node0 alloc',
        'kern: numa node1 alloc',
        'kern: thp collapse ok',
        'kern: huge page alloc ok',
        'sys: daemon bci_d up',
        'sys: daemon net_d up',
        'sys: daemon sec_d up',
        'sys: daemon db_d up',
        'sys: daemon jit_d up',
        'sys: fork pid=9144 ok',
        'sys: exec bci_daemon ok',
        'sys: exec netd ok',
        'sys: sync ok',
        'sys: fsync pid=8812 ok',
        'sys: watchdog pet ok',
        'sys: watchdog expire!',
        'sys: reboot pending',
        'sys: shutdown signal',
        'sys: service restart ok',
        'sys: service fail x3',
        'sys: socket create ok',
        'sys: socket bind ok',
        'sys: socket listen ok',
        'sys: accept conn ok',
        'sys: close fd=7 ok',
        'sys: read fd=3 512B ok',
        'sys: write fd=4 256B ok',
        'sys: ioctl 0x5401 ok',
        'sys: mlock 4096 ok',
        'sys: chroot ok',
        'sys: setuid 1001 ok',
        'sys: setgid 1001 ok',
        'sys: capget ok',
        'sys: capset drop ok',
        'sys: prctl PR_SET_NO_DUMPABLE',
        'sys: prctl PR_SET_PDEATHSIG',
        'db: edge_insert n=5 ok',
        'db: edge_insert n=12 ok',
        'db: edge_delete n=2 ok',
        'db: node_insert ok',
        'db: node_delete ok',
        'db: node_lookup ok',
        'db: index_build ok',
        'db: index_update ok',
        'db: txn begin ok',
        'db: txn commit ok',
        'db: txn rollback ok',
        'db: wal flush ok',
        'db: wal checkpoint ok',
        'db: wal rotate ok',
        'db: vacuum start ok',
        'db: vacuum complete ok',
        'db: snapshot create ok',
        'db: snapshot restore ok',
        'db: repl sync ok',
        'db: repl lag=12ms',
        'db: repl lag=450ms warn',
        'db: cache hit ratio=0.94',
        'db: cache evict n=8',
        'db: query 2.1ms ok',
        'db: query 44ms slow',
        'db: query plan scan',
        'db: query plan index',
        'db: page dirty=128',
        'db: page flush ok',
        'db: page alloc ok',
        'db: integrity check ok',
        'db: integrity fail!',
        'db: corruption detect!',
        'db: repair attempt ok',
        'db: lock acquire ok',
        'db: lock timeout!',
        'db: deadlock abort',
        'db: row count=44120',
        'db: row count=9871',
        'sec: audit event ok',
        'sec: audit write ok',
        'sec: audit flush ok',
        'sec: policy check ok',
        'sec: policy deny!',
        'sec: vault seal ok',
        'sec: vault unseal ok',
        'sec: vault read ok',
        'sec: vault write ok',
        'sec: cap check ok',
        'sec: cap deny!',
        'sec: cert verify ok',
        'sec: cert expire warn',
        'sec: cert revoke ok',
        'sec: token verify ok',
        'sec: token expire ok',
        'sec: token refresh ok',
        'sec: token invalid!',
        'sec: role check ok',
        'sec: role deny!',
        'sec: scan clean ok',
        'sec: scan threat!',
        'sec: ids alert L2',
        'sec: ids alert L4',
        'sec: ids clear ok',
        'sec: firewall allow ok',
        'sec: firewall deny!',
        'sec: firewall update ok',
        'sec: key rotate ok',
        'sec: key expire ok',
        'sec: key derive ok',
        'sec: key import ok',
        'sec: key delete ok',
        'sec: intrusion attempt!',
        'sec: intrusion block ok',
        'sec: escalation attempt!',
        'sec: escalation block ok',
        'sec: exfil detect!',
        'sec: exfil block ok',
        'sec: rootkit sig match!',
        'sec: rootkit quarantine',
        'crypt: aes256 enc ok',
        'crypt: aes256 dec ok',
        'crypt: chacha20 enc ok',
        'crypt: chacha20 dec ok',
        'crypt: hmac-sha256 ok',
        'crypt: hmac verify ok',
        'crypt: hmac mismatch!',
        'crypt: ecdh derive ok',
        'crypt: ecdh keygen ok',
        'crypt: rng seed ok',
        'crypt: rng reseed ok',
        'crypt: rng entropy ok',
        'crypt: rng low entropy!',
        'crypt: pbkdf2 ok 100k',
        'crypt: pbkdf2 fail!',
        'crypt: rsa sign ok',
        'crypt: rsa verify ok',
        'crypt: rsa fail!',
        'crypt: ed25519 sign ok',
        'crypt: ed25519 verify ok',
        'crypt: ed25519 fail!',
        'crypt: x25519 dh ok',
        'crypt: sha256 hash ok',
        'crypt: sha512 hash ok',
        'crypt: blake3 hash ok',
        'crypt: gcm tag ok',
        'crypt: gcm tag fail!',
        'crypt: nonce wrap warn!',
        'crypt: key_schedule ok',
        'crypt: iv generate ok',
        'glmz: gate 44 check ok',
        'glmz: gate 44 deny!',
        'glmz: gate 12 check ok',
        'glmz: gate 12 deny!',
        'glmz: gate 7 check ok',
        'glmz: gate 7 deny!',
        'glmz: gate 9 check ok',
        'glmz: dist4 node up',
        'glmz: dist7 node up',
        'glmz: dist9 node up',
        'glmz: dist12 node up',
        'glmz: dist4 node down!',
        'glmz: dist7 node down!',
        'glmz: enf beacon active',
        'glmz: enf beacon clear',
        'glmz: dark node 10.44.7.99',
        'glmz: dark node hidden',
        'glmz: relay up ok',
        'glmz: relay down!',
        'glmz: relay switch ok',
        'glmz: freelancer 0x3301',
        'glmz: freelancer 0x7712',
        'glmz: freelancer 0x4492',
        'glmz: freelancer 0x1187',
        'glmz: id suspend 0x3301!',
        'glmz: id clear 0x3301 ok',
        'glmz: lockdown district7!',
        'glmz: lockdown clear ok',
        'glmz: border cross ok',
        'glmz: border deny!',
        'glmz: topology update ok',
        'glmz: topology gap detect',
        'glmz: comms encrypted ok',
        'glmz: comms intercept!',
        'glmz: mesh route ok',
        'glmz: mesh route fail',
        'corp: mirror sync ok',
        'corp: mirror sync fail',
        'corp: enclave join ok',
        'corp: enclave leave ok',
        'corp: audit log write ok',
        'corp: audit log gap!',
        'corp: policy v4.2 load',
        'corp: policy deny uid=3301',
        'corp: key rotate ok',
        'corp: key rotate fail!',
        'corp: enforcer ping ok',
        'corp: enforcer alert!',
        'corp: monitor event ok',
        'corp: monitor alert!',
        'corp: sync delta ok',
        'corp: sync conflict!',
        'corp: enclave attest ok',
        'corp: enclave attest fail',
        'corp: license check ok',
        'corp: license revoke!',
        'corp: territorial ok',
        'corp: territorial dispute!',
        'mem: alloc 4096 ok',
        'mem: alloc 65536 ok',
        'mem: alloc fail ENOMEM',
        'mem: free 4096 ok',
        'mem: mmap anon ok',
        'mem: mmap file ok',
        'mem: rss 128M ok',
        'mem: rss 1.9G warn!',
        'mem: slab alloc ok',
        'mem: slab free ok',
        'mem: slab cache full!',
        'mem: numa node0 ok',
        'mem: numa node1 ok',
        'mem: numa migrate ok',
        'mem: oom adj=-17',
        'mem: oom score=0',
        'mem: oom kill pid=9144!',
        'mem: reclaim 32M ok',
        'mem: dirty pages 512',
        'mem: dirty flush ok',
        'mem: hugepage alloc ok',
        'mem: hugepage fail!',
        'mem: thp split ok',
        'mem: swap out 8p',
        'mem: swap in 4p',
        'mem: balloon inflate ok',
        'mem: balloon deflate ok',
        'mem: guard page hit!',
        'mem: overflow detect!',
        'mem: underflow detect!',
        'fs: open ok fd=7',
        'fs: open fail ENOENT',
        'fs: fsync fd=7 ok',
        'fs: fsync fail EIO',
        'fs: inode alloc ok',
        'fs: inode free ok',
        'fs: dentry cache ok',
        'fs: dentry evict ok',
        'fs: dirty pages=256',
        'fs: dirty flush ok',
        'fs: read 4096B ok',
        'fs: write 4096B ok',
        'fs: write fail EIO!',
        'fs: mkdir ok',
        'fs: unlink ok',
        'fs: rename ok',
        'fs: chmod ok',
        'fs: chown ok',
        'fs: statvfs ok',
        'fs: quota exceed!',
        'fs: journal commit ok',
        'fs: journal abort!',
        'fs: snapshot ok',
        'fs: xattr set ok',
        'fs: xattr get ok',
        'arc: lz4 ratio=3.4 ok',
        'arc: lz4 ratio=2.1 ok',
        'arc: zstd ratio=4.2 ok',
        'arc: zstd ratio=3.8 ok',
        'arc: delta_encode ok',
        'arc: delta_decode ok',
        'arc: delta base miss',
        'arc: integrity sha256 ok',
        'arc: integrity sha256 fail!',
        'arc: index update ok',
        'arc: index corrupt!',
        'arc: extract ok',
        'arc: extract fail!',
        'arc: compress blk=128K',
        'arc: compress blk=64K',
        'arc: stream ok',
        'arc: stream abort!',
        'arc: checksum ok',
        'arc: checksum fail!',
        'arc: purge old ok',
        'jit: compile ok depth=4',
        'jit: compile fail depth=8',
        'jit: trace hot 0x5582',
        'jit: trace cold 0x1f00',
        'jit: inline depth=2 ok',
        'jit: inline depth=5 ok',
        'jit: deopt guard ok',
        'jit: deopt guard miss!',
        'jit: patch ok',
        'jit: patch fail!',
        'jit: evict code ok',
        'jit: cache full evict',
        'jit: cache hit ok',
        'jit: cache miss ok',
        'jit: tiered L1 ok',
        'jit: tiered L2 ok',
        'jit: tiered L3 ok',
        'jit: osr entry ok',
        'jit: bailout ok',
        'jit: stub call ok',
        'ipc: shm_create key=0x7712',
        'ipc: shm_detach key=0x7712',
        'ipc: mq_open qid=5 ok',
        'ipc: mq_recv qid=5 ok',
        'ipc: mq_close qid=5 ok',
        'ipc: sem_init set=1 ok',
        'ipc: sem_post set=1 ok',
        'ipc: sem_wait set=1 ok',
        'ipc: pipe create ok',
        'ipc: pipe read ok',
        'ipc: pipe write ok',
        'ipc: pipe close ok',
        'ipc: sock connect ok',
        'ipc: sock send ok',
        'ipc: sock recv ok',
        'ipc: sock close ok',
        'ipc: fifo open ok',
        'ipc: fifo read ok',
        'ipc: signal send ok',
        'ipc: signal recv ok',
        'sched: preempt ok pid=8812',
        'sched: preempt ok pid=9144',
        'sched: cgroup limit hit',
        'sched: cgroup ok',
        'sched: load avg 1m=2.4',
        'sched: load avg 5m=3.1',
        'sched: load avg 15m=2.8',
        'sched: migrate pid=3301',
        'sched: deadline ok',
        'sched: realtime ok',
        'sched: idle balance ok',
        'sched: throttle group ok',
        'sched: throttle group!',
        'sched: latency 4ms ok',
        'sched: latency 22ms warn',
        'sched: wakeup ok',
        'sched: sleep ok',
        'sched: yield ok',
        'sched: priority boost',
        'sched: priority restore',
        'pci: dma map ok',
        'pci: dma unmap ok',
        'pci: dma err!',
        'pci: dev 0:1c.0 ok',
        'pci: dev 0:1c.0 err!',
        'pci: msi setup ok',
        'pci: msi fire ok',
        'usb: dev 3-2.1 ok',
        'usb: dev 3-2.1 detach',
        'usb: xfer 1024B ok',
        'usb: xfer timeout!',
        'usb: reset ok',
        'usb: reset fail!',
        'hw: sensor temp=44C ok',
        'hw: sensor temp=78C warn',
        'hw: watchdog arm ok',
        'hw: watchdog bark!',
        'hw: gpio 12 set ok',
        'hw: gpio 14 clear ok',
        'hw: pwm duty=0.5 ok',
        'hw: spi xfer ok',
        'hw: i2c addr=0x48 ok',
        'hw: i2c nack!',
        'hw: uart rx ok',
        'hw: uart overflow!',
        'bci: ring5 impedance ok',
        'bci: ring6 impedance ok',
        'bci: ring7 impedance ok',
        'bci: drift correct ok',
        'bci: drift exceed warn',
        'bci: latency 3ms ok',
        'bci: latency 18ms warn',
        'bci: sync epoch=4413',
        'bci: sync epoch=4414',
        'bci: sync epoch=4415',
        'bci: sync epoch=4416',
        'bci: sync epoch=4417',
        'bci: sync epoch=4418',
        'bci: sync epoch=4419',
        'bci: sync epoch=4420',
        'bci: operator auth ok',
        'bci: operator deny!',
        'bci: foreign sig detect!',
        'bci: foreign sig block ok',
        'bci: waveform ok',
        'bci: waveform corrupt!',
        'bci: artifact removed ok',
        'bci: channel map ok',
        'bci: channel map err!',
        'bci: threshold adapt ok',
        'bci: threshold static ok',
        'bci: model update ok',
        'bci: model rollback ok',
        'bci: session start ok',
        'bci: session end ok',
        'bci: session log write',
        'net.tx corp-mirror:8443 800B',
        'net.rx corp-mirror:8443 600B',
        'net.tx 203.0.113.9:443 1400B',
        'net.rx 203.0.113.9:443 200B',
        'net.tx 198.51.100.4:9000 512B',
        'net.rx 198.51.100.4:9000 256B',
        'net.mtu 1500 ok',
        'net.mtu 9000 jumbo ok',
        'net.frag id=0x3a ok',
        'net.icmp echo ok',
        'net.icmp unreachable!',
        'net.arp resolve ok',
        'net.arp poison detect!',
        'net.ndp resolve ok',
        'net.ipv6 flow ok',
        'net.bgp update recv',
        'net.bgp withdraw recv',
        'net.ospf hello ok',
        'net.quic stream ok',
        'net.quic stream close',
        'net.http2 push ok',
        'net.http2 rst recv',
        'net.grpc call ok',
        'net.grpc timeout!',
        'net.websocket upgrade ok',
        'net.websocket ping ok',
        'net.websocket close ok',
        'sec: hsm connect ok',
        'sec: hsm derive ok',
        'sec: hsm sign ok',
        'sec: hsm fail!',
        'sec: acl check ok',
        'sec: acl deny!',
        'sec: acl update ok',
        'sec: mfa verify ok',
        'sec: mfa fail!',
        'sec: session create ok',
        'sec: session expire ok',
        'sec: session revoke ok',
        'sec: pin verify ok',
        'sec: pin fail!',
        'sec: biometric match ok',
        'sec: biometric fail!',
        'sec: threat score=0.12',
        'sec: threat score=0.87!',
        'sec: anomaly detect!',
        'sec: anomaly clear ok',
        'db: graph traverse ok',
        'db: graph traverse n=44',
        'db: graph path found',
        'db: graph path none',
        'db: graph cycle detect',
        'db: graph cycle break',
        'db: node_update ok',
        'db: node_merge ok',
        'db: edge_update ok',
        'db: edge_merge ok',
        'db: property set ok',
        'db: property del ok',
        'db: label add ok',
        'db: label del ok',
        'db: full_scan n=9871',
        'db: partial_scan n=412',
        'db: index_scan n=88',
        'db: merge sort ok',
        'db: hash join ok',
        'db: nested loop ok',
        'db: stats update ok',
        'db: stats stale warn',
        'corp: sync interval 30s',
        'corp: sync interval 5s',
        'corp: heartbeat ok',
        'corp: heartbeat miss!',
        'corp: config push ok',
        'corp: config pull ok',
        'corp: config reject!',
        'corp: identity check ok',
        'corp: identity deny!',
        'corp: credential ok',
        'corp: credential expire!',
        'corp: enforce cmd recv',
        'corp: enforce lockout ok',
        'corp: enforce lift ok',
        'glmz: time sync ok',
        'glmz: time drift +12ms',
        'glmz: time drift -8ms',
        'glmz: ntp ok',
        'glmz: ntp fail!',
        'glmz: freq 50.00Hz ok',
        'glmz: power stable ok',
        'glmz: power unstable!',
        'glmz: sensor array ok',
        'glmz: sensor fault d7',
        'glmz: sensor repair ok',
        'glmz: corridor open ok',
        'glmz: corridor seal!',
        'glmz: fab license ok',
        'glmz: fab license revoke!',
        'glmz: crest key ok',
        'glmz: crest key revoke!',
        'glmz: operator track ok',
        'glmz: operator evade!',
        'glmz: triangulate ok',
        'bci: impedance check ok',
        'bci: impedance fail ring2',
        'bci: signal quality 0.94',
        'bci: signal quality 0.41!',
        'bci: snr=28dB ok',
        'bci: snr=11dB warn',
        'bci: snr=6dB fail!',
        'bci: band_power ok',
        'bci: band_power low warn',
        'bci: event_rate 48Hz ok',
        'bci: event_rate 120Hz warn',
        'bci: event_rate overflow!',
        'bci: grip tight detect',
        'bci: grip loose detect',
        'bci: grip neutral ok',
        'bci: release detect ok',
        'bci: twitch filter ok',
        'bci: tremor filter ok',
        'bci: artifact epoch skip',
        'bci: artifact flag ok',
        'rbs: chamber check ok',
        'rbs: chamber empty!',
        'rbs: jam detect!',
        'rbs: jam clear ok',
        'rbs: misfire detect!',
        'rbs: misfire recover ok',
        'rbs: primer ok',
        'rbs: safety on ok',
        'rbs: safety off ok',
        'rbs: discharge cycle ok',
        'rbs: discharge blocked!',
        'rbs: feed interrupt!',
        'rbs: feed resume ok',
        'rbs: bolt cycle ok',
        'rbs: bolt jam!',
        'rbs: extractor ok',
        'rbs: ejector ok',
        'rbs: trigger check ok',
        'rbs: trigger fail!',
        'rbs: selector alpha ok',
        'net.tx sentinel:8443 640B',
        'net.rx sentinel:8443 320B',
        'net.tx beacon:9000 128B',
        'net.rx beacon:9000 64B',
        'net.tx ledger:5555 1024B',
        'net.rx ledger:5555 512B',
        'net.tx vault:8080 256B',
        'net.rx vault:8080 128B',
        'net.tx cipher:4433 768B',
        'net.rx cipher:4433 384B',
        'net.latency 2ms ok',
        'net.latency 44ms warn',
        'net.latency 180ms fail',
        'net.jitter 0.4ms ok',
        'net.jitter 8ms warn',
        'net.loss 0.001 ok',
        'net.loss 0.05 warn',
        'net.loss 0.20 fail!',
        'net.bandwidth 100Mbps ok',
        'net.bandwidth 1Gbps ok',
        'net.bandwidth 10Mbps low',
        'kern: module load ok',
        'kern: module unload ok',
        'kern: module deny!',
        'kern: syscall allow ok',
        'kern: syscall deny!',
        'kern: execve ok',
        'kern: clone ok',
        'kern: chdir ok',
        'kern: chroot ok',
        'kern: pivot_root ok',
        'kern: unshare ok',
        'kern: setns ok',
        'kern: iopl set ok',
        'kern: iopl deny!',
        'kern: mprotect ok',
        'kern: mprotect deny!',
        'kern: brk ok',
        'kern: mremap ok',
        'kern: remap_file_pages ok',
        'kern: userfaultfd ok',
        'sys: cron job ok',
        'sys: cron job fail!',
        'sys: timer create ok',
        'sys: timer expire ok',
        'sys: timer cancel ok',
        'sys: signal handler ok',
        'sys: signal mask ok',
        'sys: signal unblock ok',
        'sys: poll fd=7 ok',
        'sys: epoll event ok',
        'sys: epoll timeout ok',
        'sys: select ok',
        'sys: eventfd ok',
        'sys: timerfd ok',
        'sys: signalfd ok',
        'sys: pidfd ok',
        'sys: memfd create ok',
        'sys: memfd seal ok',
        'sys: userfaultfd reg ok',
        'sys: perf event ok',
        'mem: stack grow ok',
        'mem: stack overflow!',
        'mem: heap grow ok',
        'mem: heap shrink ok',
        'mem: mlock ok',
        'mem: munlock ok',
        'mem: madvise ok',
        'mem: mincore ok',
        'mem: msync ok',
        'mem: mprotect ok',
        'mem: mprotect fail!',
        'mem: pkey ok',
        'mem: pkey access deny!',
        'mem: asan report!',
        'mem: ubsan report!',
        'mem: valgrind clean',
        'mem: valgrind error!',
        'mem: kasan report!',
        'mem: kasan clear ok',
        'mem: leak detect!',
        'crypt: kdf ok',
        'crypt: kdf fail!',
        'crypt: salt gen ok',
        'crypt: iv gen ok',
        'crypt: nonce gen ok',
        'crypt: tag verify ok',
        'crypt: tag mismatch!',
        'crypt: poly1305 ok',
        'crypt: poly1305 fail!',
        'crypt: siphash ok',
        'crypt: siphash fail!',
        'crypt: scrypt ok 16M',
        'crypt: argon2id ok',
        'crypt: argon2id fail!',
        'crypt: bcrypt ok',
        'crypt: bcrypt fail!',
        'crypt: seal ok',
        'crypt: unseal ok',
        'crypt: unseal fail!',
        'crypt: padding ok',
        'arc: huffman ok',
        'arc: huffman fail!',
        'arc: snappy ratio=2.8',
        'arc: brotli ratio=5.1',
        'arc: zlib ratio=3.3',
        'arc: xz ratio=6.2',
        'arc: lzma ratio=5.8',
        'arc: patch apply ok',
        'arc: patch apply fail!',
        'arc: manifest verify ok',
        'arc: manifest fail!',
        'arc: rollback ok',
        'arc: rollback fail!',
        'arc: catalog update ok',
        'arc: catalog search ok',
        'arc: expiry check ok',
        'arc: expiry purge ok',
        'arc: dedup detect ok',
        'arc: dedup save 12M',
        'arc: cold storage ok',
        'jit: stub icall ok',
        'jit: stub vcall ok',
        'jit: stub dcall ok',
        'jit: regalloc ok',
        'jit: regalloc spill ok',
        'jit: constant fold ok',
        'jit: dead code elim ok',
        'jit: loop unroll ok',
        'jit: loop vectorize ok',
        'jit: loop peel ok',
        'jit: branch predict ok',
        'jit: branch mispred',
        'jit: function inline ok',
        'jit: function split ok',
        'jit: tail call ok',
        'jit: tail call elim ok',
        'jit: profile load ok',
        'jit: profile save ok',
        'jit: profile hot',
        'jit: profile cold',
        'ipc: semaphore ok',
        'ipc: semaphore timeout!',
        'ipc: mutex acquire ok',
        'ipc: mutex release ok',
        'ipc: mutex contended',
        'ipc: rwlock read ok',
        'ipc: rwlock write ok',
        'ipc: rwlock contended',
        'ipc: condvar wait ok',
        'ipc: condvar signal ok',
        'ipc: condvar broadcast ok',
        'ipc: barrier reach ok',
        'ipc: barrier wait ok',
        'ipc: futex wait ok',
        'ipc: futex wake ok',
        'ipc: futex contended',
        'ipc: spinlock ok',
        'ipc: spinlock contended',
        'ipc: atomic cmpxchg ok',
        'ipc: atomic fetch_add ok',
        'sched: nice -5 ok',
        'sched: nice +5 ok',
        'sched: ionice set ok',
        'sched: ioprio set ok',
        'sched: affinity cpu0 ok',
        'sched: affinity cpu3 ok',
        'sched: isolation ok',
        'sched: nohz ok',
        'sched: rcu barrier ok',
        'sched: srcu ok',
        'sched: rcu_read_lock ok',
        'sched: context switch ok',
        'sched: voluntary yield ok',
        'sched: involuntary preempt',
        'sched: workqueue ok',
        'sched: workqueue flush ok',
        'sched: kthread ok',
        'sched: kthread stop ok',
        'sched: softirq balance ok',
        'sched: napi poll ok',
        'glmz: grid sector 7A ok',
        'glmz: grid sector 4C ok',
        'glmz: grid sector 12B ok',
        'glmz: grid sector 9D ok',
        'glmz: checkpoint pass ok',
        'glmz: checkpoint fail!',
        'glmz: cam feed d7 ok',
        'glmz: cam feed d4 ok',
        'glmz: cam feed offline!',
        'glmz: audio monitor ok',
        'glmz: audio anomaly!',
        'glmz: bio scan pass ok',
        'glmz: bio scan fail!',
        'glmz: rf tag read ok',
        'glmz: rf tag unknown!',
        'glmz: permit valid ok',
        'glmz: permit expired!',
        'glmz: permit revoked!',
        'glmz: zone A access ok',
        'glmz: zone B access deny!',
        'corp: siem event ok',
        'corp: siem alert L2!',
        'corp: siem alert L4!',
        'corp: siem clear ok',
        'corp: dlp check ok',
        'corp: dlp block!',
        'corp: edr scan ok',
        'corp: edr detect!',
        'corp: edr quarantine ok',
        'corp: patch apply ok',
        'corp: patch fail!',
        'corp: vuln scan ok',
        'corp: vuln detect!',
        'corp: vuln remediate ok',
        'corp: incident open',
        'corp: incident close ok',
        'corp: forensic collect ok',
        'corp: forensic export ok',
        'corp: chain custody ok',
        'corp: chain custody fail!',
        // 40 new
        'dist12: relay → dist7 handoff ok',
        'bci: ring4 impedance 18kΩ within spec',
        'rbs: detent-cam 12ms lock ok',
        'hkb: fluid reservoir 88% full',
        'sec: vault key 0xBE04 re-sealed ok',
        'net.rx 10.44.12.3:8181 payload 88B ok',
        'kern: cpu0 load 4.4 temp 71C ok',
        'db: vacuum freed 1.2MB in 8s ok',
        'audit: uid=3301 zone-B entry logged',
        'jit: evict fn@0x44a2 cold 44 calls',
        'arc: delta-pack ratio=3.1 ok',
        'vm: remap 0x7ffe4100 prot=r-- ok',
        'ipc: proc/3301→proc/8812 msg ok',
        'lib: dlclose libneural.so.3 ok',
        'sig: SIGTERM pid:3301 → handled',
        'tls: session 0x44f2 renegotiated ok',
        'bci: batt 73% est 4.4h remain',
        'rbs: port-alpha confirmed 2.4ms',
        'sec: token 0xBE04 refreshed ok',
        'kern: softirq NET_RX 12Kpps ok',
        'dist9: node heartbeat 3.2ms ok',
        'bci: epoch=9 acc=98.1% commit',
        'rbs: yellow-led transit active',
        'hkb: piston rebound 8ms ok',
        'sec: enclave attest 0x9f3a ok',
        'net.syn 10.12.0.44:4412 ESTABLISHED',
        'db: edge-idx rebuild 4192 nodes ok',
        'glmz: gate-7 transit 0x4492 ok',
        'corp: incident-4412 closed ok',
        'bci: grip-cls conf=0.94 ok',
        'rbs: disc alpha→beta 38ms ok',
        'hkb: impulse spread 8ms→114ms ok',
        'kern: ksoftirqd cpu1 SCHED_OTHER',
        'net.rx 172.16.0.3:9090 HTTP 200',
        'sec: audit-trail 0x3301 flushed',
        'bci: calibrate ring3 Δ=+0.011',
        'ipc.gate: 0x22 [relay fwd enc]',
        'db: txn 0xDC44 commit 3.1ms ok',
        'arc: snapshot 0x44a2 verified',
        'jit: patch fn@0x5582 hot 812 calls',
        'bci: pair-key 0xA7F2 accepted',
        'rbs: spring-load 2.1N nominal',
        'hkb: gas-vent cycle ok 14ms',
        'net: beacon 0xBE04 heartbeat ok',
        'dist4: fab node up lat=9ms',
        'sec: kdf iter=200k derive ok',
        'bci: sweep-cls confirmed 0xAB ok',
        'kern: rcu grace period 2.4ms',
        'db: bloom-filter hit rate 91%',
        'corp: key-rotation vault ok',
    ];

    // ── Exit results ────────────────────────────────────────────────────────
    var OK_RESULTS = [
        'OK   exit:0  Φ',
        'OK   sync verified',
        'OK   handshake accepted',
        'OK   gate:open',
        'OK   0 faults',
        'OK   dispatch complete',
        'OK   checksum pass',
        'OK   commit 0x3af7',
        'OK   calibration saved',
        'OK   relay ack',
        'OK   token valid',
        'OK   neural lock',
        'OK   4 pages reclaimed',
        'OK   cert verified',
        'OK   latency within SLA',
    ];

    var ERR_RESULTS = [
        'ERR  timeout gate:0x1b',
        'ERR  seg fault cr2=null',
        'ERR  auth rejected',
        'ERR  ipc EOF unexpected',
        'ERR  ENOMEM alloc failed',
        'ERR  checksum mismatch',
        'ERR  sig lost pid:8812',
        'ERR  neural desync',
        'ERR  rbs jam detected',
        'ERR  relay no ack 3x',
        'ERR  cert revoked',
        'ERR  heap corrupted',
        'ERR  lock timeout 500ms',
        'ERR  bci pair lost',
        'ERR  port beta empty',
    ];

    // ── Fatal error dialog ──────────────────────────────────────────────────
    var FATAL_TITLES = [
        // original 7
        'FATAL — kernel panic',
        'FATAL — process crash',
        'CRITICAL — unhandled exception',
        'PANIC — memory violation',
        'FATAL — core dump',
        'CRITICAL — stack overflow',
        'FATAL — assertion failed',
        // 100 new entries
        'FATAL — bci_daemon abort',
        'FATAL — neural sync lost',
        'FATAL — rbs motor fault',
        'FATAL — grip classifier panic',
        'FATAL — bim chip unresponsive',
        'FATAL — port alpha jam',
        'FATAL — port beta misfire',
        'FATAL — hydraulic buffer fail',
        'FATAL — neural key revoked',
        'FATAL — cortex signal lost',
        'CRITICAL — heap corruption',
        'CRITICAL — use-after-free',
        'CRITICAL — buffer overflow',
        'CRITICAL — integer overflow',
        'CRITICAL — null dereference',
        'CRITICAL — race condition',
        'CRITICAL — deadlock detected',
        'CRITICAL — ipc gate closed',
        'CRITICAL — auth token expired',
        'CRITICAL — tls cert revoked',
        'CRITICAL — db corruption',
        'CRITICAL — wal log truncated',
        'CRITICAL — index corruption',
        'CRITICAL — disk full abort',
        'CRITICAL — fd limit exceeded',
        'CRITICAL — net partition',
        'CRITICAL — relay timeout 3x',
        'CRITICAL — glmz node offline',
        'CRITICAL — corp sync lost',
        'CRITICAL — enclave attest fail',
        'PANIC — double free',
        'PANIC — stack smash',
        'PANIC — wild pointer write',
        'PANIC — irq storm',
        'PANIC — vm corruption',
        'PANIC — slab underflow',
        'PANIC — dma overrun',
        'PANIC — watchdog timeout',
        'PANIC — nmi received',
        'PANIC — cpu lockup',
        'ABORT — assertion gate.cc',
        'ABORT — invariant violated',
        'ABORT — contract breach',
        'ABORT — precondition fail',
        'ABORT — postcondition fail',
        'ABORT — type confusion',
        'ABORT — format string fault',
        'ABORT — canary smashed',
        'ABORT — signal 11 SIGSEGV',
        'ABORT — signal 6 SIGABRT',
        'ABORT — signal 4 SIGILL',
        'ABORT — signal 8 SIGFPE',
        'ERROR — out of memory',
        'ERROR — device io fault',
        'ERROR — disk read error',
        'ERROR — journal replay fail',
        'ERROR — snapshot corrupt',
        'ERROR — replication lag',
        'ERROR — network unreachable',
        'ERROR — dns resolution fail',
        'ERROR — tls handshake fail',
        'ERROR — certificate expired',
        'KERNEL — divide by zero',
        'KERNEL — bad page table',
        'KERNEL — invalid opcode',
        'KERNEL — general protection',
        'KERNEL — page fault #14',
        'KERNEL — machine check',
        'KERNEL — oops in slab',
        'KERNEL — kasan report',
        'KERNEL — kcsan data race',
        'KERNEL — lockdep violation',
        'CREST — rbs disc shattered',
        'CREST — bim chip fused',
        'CREST — neural key tamper',
        'CREST — hkb piston seized',
        'CREST — battery critical 0%',
        'GLMZ — gate blacklist',
        'GLMZ — border lockdown',
        'GLMZ — district feed cut',
        'GLMZ — enforcement purge',
        'GLMZ — dark node exposure',
        'CORP — policy violation',
        'CORP — audit trail break',
        'CORP — enclave compromise',
        'CORP — zero-day triggered',
        'CORP — exfil detected',
        'BCI — motor signal flood',
        'BCI — calibration corrupt',
        'BCI — foreign key injection',
        'BCI — denial-of-sensation',
        'BCI — interface lockout',
        'SEC — intrusion confirmed',
        'SEC — privilege escalation',
        'SEC — lateral move detected',
        'SEC — exfil in progress',
        'SEC — rootkit signature',
        'FATAL — bci ring4 contact lost',
        'FATAL — grip classifier crash',
        'FATAL — neural_rx overflow',
        'FATAL — bim_proc abort',
        'FATAL — cortex_sync timeout',
        'FATAL — rbs disc stuck',
        'FATAL — hkb piston jam',
        'FATAL — slug_feed empty',
        'FATAL — buck_feed jam',
        'FATAL — weapon thermal',
        'FATAL — net daemon abort',
        'FATAL — db corruption',
        'FATAL — sec vault fail',
        'FATAL — crypt engine abort',
        'FATAL — arc integrity fail',
        'FATAL — jit compile abort',
        'FATAL — ipc deadlock',
        'FATAL — sched starvation',
        'FATAL — fs journal abort',
        'FATAL — mem oom kill',
        'CRITICAL — bci key mismatch',
        'CRITICAL — rbs port fail',
        'CRITICAL — hkb buffer fail',
        'CRITICAL — neural flood',
        'CRITICAL — grip signal lost',
        'CRITICAL — bim classify fail',
        'CRITICAL — cortex desync',
        'CRITICAL — calibrate corrupt',
        'CRITICAL — epoch overflow',
        'CRITICAL — sweep lock fail',
        'CRITICAL — precision lock fail',
        'CRITICAL — weapon lockout',
        'CRITICAL — net bridge down',
        'CRITICAL — db txn abort',
        'CRITICAL — sec policy fail',
        'CRITICAL — crypt key corrupt',
        'CRITICAL — mem heap corrupt',
        'CRITICAL — fs dirty flush fail',
        'CRITICAL — sched deadline miss',
        'CRITICAL — ipc mutex corrupt',
        'PANIC — kernel null deref',
        'PANIC — kernel stack overflow',
        'PANIC — kernel double free',
        'PANIC — kernel use-after-free',
        'PANIC — kernel wild pointer',
        'PANIC — kernel irq storm',
        'PANIC — kernel rcu stall',
        'PANIC — kernel watchdog bark',
        'PANIC — kernel vm fault',
        'PANIC — kernel slab corrupt',
        'PANIC — kernel oops #5',
        'PANIC — kernel oops #11',
        'PANIC — kernel oops #14',
        'PANIC — kernel oops #7',
        'PANIC — kernel oops #9',
        'PANIC — kernel cpu lockup',
        'PANIC — kernel nmi',
        'PANIC — kernel mce',
        'PANIC — kernel ecc fail',
        'PANIC — kernel panic boot',
        'ABORT — signal 11 SIGSEGV',
        'ABORT — signal 6 SIGABRT',
        'ABORT — signal 4 SIGILL',
        'ABORT — signal 8 SIGFPE',
        'ABORT — signal 7 SIGBUS',
        'ABORT — signal 9 SIGKILL',
        'ABORT — signal 15 SIGTERM',
        'ABORT — bci_daemon crash',
        'ABORT — rbs_ctrl crash',
        'ABORT — hkb_mon crash',
        'ABORT — sec_daemon crash',
        'ABORT — net_daemon crash',
        'ABORT — db_daemon crash',
        'ABORT — jit_daemon crash',
        'ABORT — arc_daemon crash',
        'ABORT — fs assertion fail',
        'ABORT — mem assertion fail',
        'ABORT — crypt assertion fail',
        'ABORT — net assertion fail',
        'ABORT — corp daemon crash',
        'ERROR — bci ring impedance',
        'ERROR — bci snr low',
        'ERROR — bci waveform corrupt',
        'ERROR — bci model invalid',
        'ERROR — bci session fail',
        'ERROR — rbs selector fail',
        'ERROR — rbs cam read fail',
        'ERROR — rbs thermal limit',
        'ERROR — hkb pressure low',
        'ERROR — hkb spring fail',
        'ERROR — net tls handshake',
        'ERROR — net dns resolve',
        'ERROR — net conn refused',
        'ERROR — net timeout',
        'ERROR — net rst inject',
        'ERROR — db index corrupt',
        'ERROR — db repl lag',
        'ERROR — db lock timeout',
        'ERROR — db wal overflow',
        'ERROR — db snapshot fail',
        'KERNEL — page fault #14',
        'KERNEL — page fault #11',
        'KERNEL — page fault #7',
        'KERNEL — page not present',
        'KERNEL — write protect fault',
        'KERNEL — execute disable fault',
        'KERNEL — reserved bit fault',
        'KERNEL — SMEP violation',
        'KERNEL — SMAP violation',
        'KERNEL — NX violation',
        'KERNEL — double fault',
        'KERNEL — triple fault',
        'KERNEL — GPF #13',
        'KERNEL — invalid TSS',
        'KERNEL — stack fault',
        'KERNEL — alignment check',
        'KERNEL — machine check',
        'KERNEL — divide by zero',
        'KERNEL — invalid opcode',
        'KERNEL — device not avail',
        'CREST — rbs disc shattered',
        'CREST — rbs port mismatch',
        'CREST — bim chip fail',
        'CREST — bim key invalid',
        'CREST — bim model corrupt',
        'CREST — hkb hydraulic fail',
        'CREST — hkb spring break',
        'CREST — slug feed jam',
        'CREST — chamber obstruction',
        'CREST — thermal runaway',
        'CREST — led matrix fail',
        'CREST — pin actuator fail',
        'CREST — selector corrupt',
        'CREST — calibrate fail',
        'CREST — serial verify fail',
        'CREST — firmware corrupt',
        'CREST — firmware rollback',
        'CREST — cert expired',
        'CREST — cert revoked',
        'CREST — telemetry abort',
        'GLMZ — gate blacklist',
        'GLMZ — gate offline',
        'GLMZ — gate override',
        'GLMZ — gate seizure',
        'GLMZ — gate power fail',
        'GLMZ — district lockdown',
        'GLMZ — district quarantine',
        'GLMZ — dark node exposed',
        'GLMZ — relay down',
        'GLMZ — relay intercept',
        'GLMZ — comms blocked',
        'GLMZ — comms intercept',
        'GLMZ — beacon override',
        'GLMZ — permit revoked',
        'GLMZ — freelancer blacklist',
        'GLMZ — operator tracked',
        'GLMZ — operator triangulate',
        'GLMZ — fab license revoked',
        'GLMZ — corridor seal',
        'GLMZ — power grid fail',
        'CORP — policy violation',
        'CORP — policy override',
        'CORP — policy lockout',
        'CORP — audit log tamper',
        'CORP — audit gap detected',
        'CORP — mirror sync fail',
        'CORP — mirror conflict',
        'CORP — enclave breach',
        'CORP — enclave attest fail',
        'CORP — key rotation fail',
        'CORP — credential revoked',
        'CORP — enforcement alert',
        'CORP — enforcement lockout',
        'CORP — data exfil detect',
        'CORP — data exfil blocked',
        'CORP — lateral move detect',
        'CORP — config tamper',
        'CORP — license revoked',
        'CORP — territorial dispute',
        'CORP — incident escalated',
        'BCI — motor signal flood',
        'BCI — motor signal lost',
        'BCI — motor signal corrupt',
        'BCI — neural key fail',
        'BCI — neural key mismatch',
        'BCI — neural key corrupt',
        'BCI — grip classify fail',
        'BCI — grip signal lost',
        'BCI — grip pattern foreign',
        'BCI — calibration corrupt',
        'BCI — calibration timeout',
        'BCI — calibration abort',
        'BCI — foreign key injection',
        'BCI — denial-of-sensation',
        'BCI — interface lockout',
        'BCI — operator id mismatch',
        'BCI — operator id revoked',
        'BCI — ring contact fail',
        'BCI — epoch desync',
        'BCI — buffer overflow',
        'SEC — intrusion confirmed',
        'SEC — privilege escalation',
        'SEC — lateral move detected',
        'SEC — exfil in progress',
        'SEC — rootkit active',
        'SEC — rootkit variant new',
        'SEC — keylogger detect',
        'SEC — process inject detect',
        'SEC — dll inject detect',
        'SEC — mem scrape detect',
        'SEC — credential dump',
        'SEC — pass-the-hash',
        'SEC — token forge',
        'SEC — kerberoast detect',
        'SEC — golden ticket detect',
        'SEC — silver ticket detect',
        'SEC — dcSync detect',
        'SEC — supply chain alert',
        'SEC — zero day detect',
        'SEC — exploit attempt',
        'NET — glmz relay down',
        'NET — glmz relay intercept',
        'NET — district bridge fail',
        'NET — enforcement block',
        'NET — dns poison detect',
        'NET — arp spoof detect',
        'NET — bgp hijack detect',
        'NET — traffic analysis',
        'NET — bandwidth cap hit',
        'NET — tunnel collapse',
        'NET — tls downgrade detect',
        'NET — cert spoof detect',
        'NET — sni intercept',
        'NET — deep packet inspect',
        'NET — rate limit exceed',
        'NET — ddos detect',
        'NET — syn flood detect',
        'NET — reflection attack',
        'NET — amplification detect',
        'NET — covert channel',
        'DB — index corrupt',
        'DB — wal overflow',
        'DB — txn deadlock',
        'DB — replication lag',
        'DB — snapshot corrupt',
        'DB — page checksum fail',
        'DB — vacuum abort',
        'DB — integrity check fail',
        'DB — foreign key violate',
        'DB — constraint violate',
        'DB — unique violate',
        'DB — null violate',
        'DB — type mismatch',
        'DB — overflow detect',
        'DB — underflow detect',
        'DB — injection attempt',
        'DB — query timeout',
        'DB — connection pool exhaust',
        'DB — disk full',
        'DB — log full',
        'MEM — heap corruption',
        'MEM — stack smash',
        'MEM — buffer overflow',
        'MEM — buffer underflow',
        'MEM — use-after-free',
        'MEM — double free',
        'MEM — wild pointer',
        'MEM — null deref',
        'MEM — uninitialized read',
        'MEM — format string',
        'MEM — integer overflow',
        'MEM — integer underflow',
        'MEM — type confusion',
        'MEM — race condition',
        'MEM — oom kill',
        'MEM — slab corrupt',
        'MEM — page fault',
        'MEM — guard page hit',
        'MEM — pkey violation',
        'MEM — kasan report',
        'SYS — daemon crash',
        'SYS — service restart',
        'SYS — service fail',
        'SYS — watchdog expire',
        'SYS — reboot forced',
        'SYS — shutdown abnormal',
        'SYS — fork fail',
        'SYS — exec fail',
        'SYS — socket fail',
        'SYS — pipe fail',
        'SYS — timer fail',
        'SYS — cron fail',
        'SYS — cgroup limit',
        'SYS — fd leak detect',
        'SYS — handle leak detect',
        'SYS — thread leak detect',
        'SYS — zombie accumulate',
        'SYS — orphan process',
        'SYS — init fail',
        'SYS — mount fail',
        'SIGNAL — SIGSEGV pid=8812',
        'SIGNAL — SIGABRT pid=9144',
        'SIGNAL — SIGILL pid=3301',
        'SIGNAL — SIGFPE pid=7712',
        'SIGNAL — SIGBUS pid=4492',
        'SIGNAL — SIGKILL pid=8812',
        'SIGNAL — SIGTERM pid=9144',
        'SIGNAL — SIGUSR1 abort',
        'SIGNAL — SIGUSR2 abort',
        'SIGNAL — SIGPIPE pid=3301',
        'SIGNAL — SIGHUP abort',
        'SIGNAL — SIGALRM expire',
        'SIGNAL — SIGCHLD abort',
        'SIGNAL — SIGXCPU exceeded',
        'SIGNAL — SIGXFSZ exceeded',
        'SIGNAL — SIGVTALRM expire',
        'SIGNAL — SIGPROF expire',
        'SIGNAL — SIGTRAP debug',
        'SIGNAL — SIGWINCH ignore',
        'SIGNAL — SIGSYS sandboxed',
        'RACE — data race detect',
        'RACE — lock order invert',
        'RACE — toctou detect',
        'RACE — aba problem detect',
        'RACE — write-write race',
        'RACE — read-write race',
        'RACE — iterator race',
        'RACE — destructor race',
        'RACE — init race detect',
        'RACE — singleton race',
        'RACE — cache race detect',
        'RACE — counter race detect',
        'RACE — flag race detect',
        'RACE — pointer race detect',
        'RACE — refcount race',
        'RACE — tsan report',
        'RACE — helgrind detect',
        'RACE — drd detect',
        'RACE — memcheck detect',
        'RACE — thread sanitizer',
        'LOCK — deadlock detected',
        'LOCK — livelock detected',
        'LOCK — mutex timeout',
        'LOCK — rwlock timeout',
        'LOCK — semaphore timeout',
        'LOCK — condvar timeout',
        'LOCK — barrier timeout',
        'LOCK — futex timeout',
        'LOCK — spinlock timeout',
        'LOCK — lock hierarchy violate',
        'LOCK — priority inversion',
        'LOCK — convoy detect',
        'LOCK — thundering herd',
        'LOCK — starvation detect',
        'LOCK — recursive deadlock',
        'LOCK — cross-process deadlock',
        'LOCK — distributed deadlock',
        'LOCK — order violation',
        'LOCK — cycle detect',
        'LOCK — orphan lock detect',
        'IO — disk write fail',
        'IO — disk read fail',
        'IO — disk timeout',
        'IO — disk full',
        'IO — disk bad sector',
        'IO — disk ecc fail',
        'IO — disk smart warn',
        'IO — disk smart fail',
        'IO — raid degraded',
        'IO — raid fail',
        'IO — network timeout',
        'IO — network reset',
        'IO — network unreachable',
        'IO — usb transfer fail',
        'IO — pci dma fail',
        'IO — i2c nack',
        'IO — spi timeout',
        'IO — uart overflow',
        'IO — gpio fault',
        'IO — pwm fault',
        'FATAL — bci epoch overflow',
        'FATAL — rbs emergency stop',
        'FATAL — hkb thermal limit',
        'FATAL — net corp mirror fail',
        'FATAL — sec vault locked',
        'FATAL — db disk full',
        'FATAL — mem slab exhaust',
        'FATAL — fs journal corrupt',
        'FATAL — jit code cache full',
        'FATAL — arc checksum fail',
        'CRITICAL — bci operator deny',
        'CRITICAL — rbs led fail',
        'CRITICAL — hkb spring fail',
        'CRITICAL — net beacon spoof',
        'CRITICAL — sec hsm fail',
        'CRITICAL — db graph corrupt',
        'CRITICAL — mem guard hit',
        'CRITICAL — fs inode corrupt',
        'CRITICAL — jit bail fail',
        'CRITICAL — arc delta fail',
        'PANIC — kernel watchdog nmi',
        'PANIC — kernel cpu deadlock',
        'PANIC — kernel oops cascading',
        'PANIC — kernel dma storm',
        'PANIC — kernel irq cascade',
        'PANIC — kernel io storm',
        'PANIC — kernel pcie error',
        'PANIC — kernel memory storm',
        'PANIC — kernel rcu timeout',
        'PANIC — kernel hung task',
        'ABORT — bci daemon assert',
        'ABORT — rbs controller abort',
        'ABORT — hkb monitor abort',
        'ABORT — net proxy abort',
        'ABORT — sec scanner abort',
        'ABORT — db journal abort',
        'ABORT — mem allocator abort',
        'ABORT — fs flusher abort',
        'ABORT — jit executor abort',
        'ABORT — arc verifier abort',
        'ERROR — bci contact ring3',
        'ERROR — rbs thermal trip',
        'ERROR — hkb fluid empty',
        'ERROR — net tunnel collapse',
        'ERROR — sec cert expired',
        'ERROR — db replica fail',
        'ERROR — mem pkey fault',
        'ERROR — fs sync fail',
        'ERROR — jit trace fail',
        'ERROR — arc index missing',
        'KERNEL — tlb flush fail',
        'KERNEL — cr3 invalid',
        'KERNEL — cr0 corrupt',
        'KERNEL — gdtr corrupt',
        'KERNEL — idtr corrupt',
        'KERNEL — idt gate invalid',
        'KERNEL — gdt segment fault',
        'KERNEL — ldt fault',
        'KERNEL — tss descriptor fail',
        'KERNEL — pt walk fail',
        'CREST — weapon id spoof',
        'CREST — weapon ban active',
        'CREST — telemetry block',
        'CREST — calibrate tamper',
        'CREST — round counter spoof',
        'CREST — discharge blocked',
        'CREST — sensor array fail',
        'CREST — actuator fault',
        'CREST — motor controller fail',
        'CREST — power rail fault',
        'GLMZ — sensor array fail',
        'GLMZ — corridor offline',
        'GLMZ — checkpoint breach',
        'GLMZ — zone A quarantine',
        'GLMZ — zone B lockdown',
        'GLMZ — cam offline d4',
        'GLMZ — cam offline d7',
        'GLMZ — audio anomaly d12',
        'GLMZ — bio scan fail d9',
        'GLMZ — rf jamming detect',
        'CORP — siem alert critical',
        'CORP — dlp breach detect',
        'CORP — edr isolate host',
        'CORP — vuln exploit detect',
        'CORP — zero day active',
        'CORP — supply chain breach',
        'CORP — insider threat detect',
        'CORP — data destruction detect',
        'CORP — ransomware detect',
        'CORP — apt persist detect',
        'BCI — cortex signal null',
        'BCI — cortex signal overload',
        'BCI — intent state corrupt',
        'BCI — intent transition fail',
        'BCI — bim chip overheat',
        'BCI — bim power rail fail',
        'BCI — neural band corrupt',
        'BCI — theta spike flood',
        'BCI — beta burst overflow',
        'BCI — gamma artifact flood',
        'SEC — zero day confirmed',
        'SEC — apt active detect',
        'SEC — ransomware active',
        'SEC — data wipe detect',
        'SEC — firmware tamper',
        'SEC — bootkit detect',
        'SEC — hypervisor attack',
        'SEC — vm escape detect',
        'SEC — container escape',
        'SEC — kernel exploit',
        // 40 more
        'FATAL — neural bridge dropout',   'FATAL — bci handshake refused',   'FATAL — rbs spring shatter',
        'FATAL — hkb valve blowout',       'FATAL — glmz gate auth fail',     'FATAL — relay blackhole loop',
        'FATAL — sec key store corrupt',   'FATAL — db redo log corrupt',     'FATAL — vm page table fault',
        'FATAL — jit segment overflow',    'FATAL — ipc ring overflow',       'FATAL — tls session bomb',
        'CRITICAL — bci flood attack',     'CRITICAL — rbs cam desync',       'CRITICAL — hkb rack jam',
        'CRITICAL — glmz perimeter breach','CRITICAL — corp exfil volume',    'CRITICAL — sec replay storm',
        'CRITICAL — db wal storm',         'CRITICAL — net arp poison',       'CRITICAL — vm oom cascade',
        'CRITICAL — jit cache corrupt',    'CRITICAL — ipc deadlock global',  'CRITICAL — arc block missing',
        'ABORT — bci ring6 fault',         'ABORT — rbs torque limit',        'ABORT — hkb actuator stall',
        'ABORT — glmz feed desync',        'ABORT — corp broker timeout',     'ABORT — sec vault gone',
        'ERROR — bci epoch mismatch',      'ERROR — rbs position lost',       'ERROR — hkb fluid leak',
        'ERROR — glmz relay loop',         'ERROR — corp audit missing',      'ERROR — sec cert chain broken',
        'KERNEL — slab poison detect',     'KERNEL — hpet timer fault',       'KERNEL — cpuid mismatch',
        'CREST — auth module unresponsive','GLMZ — district feed blackout',   'BCI — classifier poison inject',
        'SEC — bci exploit detected',      'PANIC — kernel dma storm',        'PANIC — irq flood 64k/s',
    ];

    var FATAL_MSGS = [
        // original 7
        'NullPointerException at 0x5582c4b8\nThread 8812 terminated — backtrace dumped\nCore written to /tmp/core.8812',
        'Segmentation fault (core dumped)\nAddress 0x00000000 not mapped\nSignal 11 (SIGSEGV) at pid:9144',
        'EXCEPTION_ACCESS_VIOLATION 0xc0000005\nRead from 0x00000000000000a8\nModule: libneural.so.3 +0x4f3a',
        'Stack smashing detected\nAbort trap: 6\nProgram: bci_daemon pid:8812',
        'Assertion failed: ptr != nullptr\nFile: gate.cc line 1403\nAborting pid:8812',
        'Out of memory: kill process 8812\noom_score_adj=0 vm_rss=1.9G\nKernel panic — not syncing',
        'Double free detected in tcache 2\nCorrupted pointer: 0x5582c4a0\nAbort trap — heap integrity check',
        // 100 new entries
        'bci_daemon: neural handshake timeout 5000ms\nGrip classifier lost contact pid:8812\nBIM chip not responding — abort',
        'BIM accuracy drop: 61.2% (threshold 80%)\nCalibration epoch corrupted block 7\nReverting to factory defaults — manual override required',
        'RBS disc rotation stall at 22ms\nExpected 38ms full cycle — motor fault\nPort selection indeterminate — weapon unsafe',
        'Cortex signal flood detected\n12,400 events/ms — saturation threshold 800\nNeural interface shutting down pid:8812',
        'Neural key 0xA3F7 rejected by corp gate\nBIM chip reflashed — foreign employer signature\noperator locked out of own weapon',
        'HKB piston seized at stroke 3.1mm\nExpected 8mm — buffer failure\nRecoil unmitigated — operator injury risk',
        'Port beta empty — slug misfire\nExtractor found no casing in chamber\nRBS returned to alpha — 0 rounds remaining',
        'use-after-free in libneural.so.3 @ 0x7f3a\nHeap metadata overwritten addr=0x5582c4a0\nAddressSanitizer: SEGV on unknown address',
        'Stack buffer overflow in grip_classify()\nReturn address overwritten with 0x4141414141\nPossible exploitation attempt — abort',
        'Kernel OOPS: unable to handle paging request\nvirtual address 0xfffffffffffffff8\nRIP: bci_core+0x3a4/0x5c0',
        'KASAN: use-after-free in bim_calibrate()\nRead of size 8 at addr ffff888103a4\nBuggy address in object freed at bim_init+0x12',
        'Lockdep violation: possible deadlock\nhold: &bci_lock → &rbs_lock\nwait: &rbs_lock → &bci_lock  (cycle)',
        'Machine check: corrected error\nBank 4 MSR 0x411 = 0xbe00000000800400\nMemory controller: DRAM ECC correctable',
        'irqbalance: cpu2 lockup — self-test fail\nNMI watchdog: BUG soft lockup CPU#2 stuck 22s\nKernel panic: not syncing',
        'TCP connection reset by enforcement node\nconn 10.44.7.3:8443 → 172.16.0.1:443\nRST injected mid-handshake — active intercept?',
        'TLS certificate verification failed\ncert: glmz.relay.dist7 CN mismatch\nchain: expired intermediate 2024-11-30',
        'GLMZ gate 0x1b: blacklist match uid=3301\nDistrict 4 access revoked enforcement order\nAll outbound routes blocked — isolated',
        'District 12 relay offline — no heartbeat 120s\nFallback route dist7→dist9 congested 88%\nPacket loss 34% — connection degraded',
        'Corporate audit trail integrity check failed\nBlock 8192 hash mismatch expected=0x3af7\nTampering suspected — incident flagged',
        'Corp enclave attestation signature invalid\nExpected PCR[7]=0x2a3f got 0x0000\nPossible hypervisor injection — lockdown',
        'DB node corruption detected page 0x3301\nFSM state INVALID for block group 7\nFilesystem unmounted — journal replay required',
        'WAL log truncated unexpectedly at lsn 0x3af7\nExpected 4096 frames found 2048\nDatabase unrecoverable without backup',
        'Heap corruption detected in db/edge_insert\nChunk size 0x80 but next_size=0x21 (mismatch)\nMalloc abort — heap metadata invalid',
        'integer overflow in pkt_len calculation\nlen=0xffff0001 overflow wraps to 1\nBuffer underrun — memory corruption imminent',
        'format string vulnerability triggered\nprintf(user_buf) — controlled format string\nprocess 9144 terminated — possible exploit',
        'Signal 11 (SIGSEGV) at address 0x10\nLikely null pointer dereference + offset\npid 8812 bci_daemon — core dumped',
        'Signal 4 (SIGILL) — illegal instruction\nInvalid opcode at RIP=0x5582c4f0\nPossible code corruption or JIT error',
        'Signal 8 (SIGFPE) — arithmetic exception\nDivide by zero at net_rate_calc+0x4a\npid:8812 — process terminated',
        'OOM killer invoked\nKilling process 8812 (bci_daemon) score=892\nMem: 2097152kB total, 12kB free',
        'Disk full — /var/log 100% capacity\nWrite failed: No space left on device\nbci calibration log truncated — data loss',
        'fd limit exceeded: ulimit -n=1024\nprocess 8812 cannot open new file descriptors\nNeural calibration file handle leak suspected',
        'inode exhaustion on /var/bci\n0 inodes remaining on filesystem\nCannot create new calibration epoch files',
        'Syscall audit: unexpected execve() by pid 9144\nParent: bci_daemon — no exec expected\nPossible code injection — process sandboxed',
        'Seccomp violation: sys_ptrace by pid:9144\nKilled by SIGSYS — sandbox policy\nIntrusion attempt logged uid=1000',
        'SELinux denial: bci_daemon → corp_vault_t\npermission read on file /var/vault/key.pem\nAVC denied — policy enforcement active',
        'capabilities violation: CAP_NET_RAW denied\npid:9144 attempted raw socket creation\nProcess killed SIGKILL — policy enforced',
        'DMA overrun detected pci 0:1b.0\nDMA write outside mapped region 0x3af7000\nIOMMU fault — device reset required',
        'USB device 3-1.2 disconnected mid-transfer\nExpected 4096B transferred 1024B\nbci interface offline — reconnect required',
        'PCIe link down: device 0:1e.0\nlink_speed 8GT/s → link_down\nbci adapter offline — kernel driver error',
        'nvme0 I/O error: LBA 0x3af7 read fail\nError code: 0x4 (aborted command)\nFilesystem marked read-only — I/O error',
        'RAID array degraded: device /dev/sdb failed\n1 of 2 mirrors offline — no redundancy\nImminent data loss — replace drive',
        'Snapshot 0x3301 integrity check failed\nChecksum mismatch block 4096 of 8192\nRestore aborted — source corrupted',
        'Replication lag exceeds threshold: 44s\nPrimary LSN 0x3af7 replica 0x3a00\nAutomatic failover blocked — manual intervention',
        'Index 0x3301 B-tree root corrupted\nPage header magic 0xDEAD expected 0x3141\nDatabase offline — rebuild from backup',
        'Journal replay failed at lsn 0x1b\nTransaction incomplete — data loss\nManual recovery required: fsck.ext4',
        'SMART threshold exceeded: reallocated sectors=512\nPredicted failure imminent drive /dev/sda\nBackup immediately — hardware failure',
        'Memory ECC uncorrectable error\nDIMM slot A1 addr=0x7ffe3200\nSystem halted — hardware fault',
        'CPU thermal throttle: package temp 94°C\nPerformance reduced — cooling system fault\nProcess bci_daemon missed 22 deadlines',
        'Watchdog timeout: bci_daemon 30s no heartbeat\nAutomatic restart failed — respawn limit 5\nManual intervention required',
        'Zombie process storm: 4092 zombies\nParent pid:8812 not reaping children\nPID table exhaustion imminent',
        'Thread 8812 blocked 5min on mutex\nPossible deadlock: holder=thread 9144\nBacktrace: bim_calibrate → rbs_select → bci_sync',
        'CORP zero-day: privilege escalation attempt\nCVE-2025-8812 libneural.so.3 heap overflow\nPatch not applied — system vulnerable',
        'Rootkit signature detected in /proc\nhidden PID 31337 not in task_struct list\nKernel integrity compromised — incident',
        'Lateral movement detected\nuid=1000 → uid=0 via CVE-2025-8812\nPivot to corp_vault — exfil in progress',
        'Intrusion confirmed: corp_enclave breached\nForeign key material extracted — 4096B\nZero-day exploit — containment failed',
        'Network partition: district 7 → district 12\nAll relay routes timed out 30s\nGLMZ topology fragmented — traffic blocked',
        'ARP spoofing detected on 10.44.7.0/24\nGateway MAC changed 3x in 60s\nMan-in-the-middle attack suspected',
        'DNS hijack: glmz.relay.7 → 10.0.0.1\nExpected 172.16.44.12 got 10.0.0.1\nPhishing redirect — connection refused',
        'SYN flood on port 8443: 48k pkt/s\nHalf-open connections=65535 — table full\nbci_daemon connection refused — service down',
        'BGP route withdrawal: 10.44.0.0/16\nGLMZ district 4 unreachable\nAll paths withdrawn — isolation detected',
        'Firewall rule collision: rule 44 vs rule 7\nOverlapping src/dst — policy undefined\nPacket dropped — connection failure',
        'SSL_CTX_new() returned NULL\nOpenSSL error: lib(20) reason(65)\nOut of memory — TLS unavailable',
        'x509 certificate chain incomplete\nMissing intermediate CA at depth 1\nPeer verification failed — connection closed',
        'OCSP responder timeout: 5000ms\nCannot verify revocation status\nCertificate rejected — paranoid policy',
        'Diffie-Hellman key too small: 512 bits\nMinimum 2048 required by policy\nHandshake aborted — weak parameters',
        'HMAC verification failed: message tampered\nExpected 0x3af7c2d1 got 0xa1b2c3d4\nReplay attack? — connection terminated',
        'AES-GCM tag mismatch — decryption failed\nCiphertext: 64 bytes at 0x5582c4a0\nData integrity compromised — discard',
        'Random number generator failure\n/dev/random blocked — entropy pool empty\nCryptographic operations suspended',
        'Kernel canary check failed: pid:9144\nExpected 0x3af7deadbeef1337\nStack corruption — process killed',
        'ASLR bypass detected: same load addr 5x\nBase 0x5582c4a0 repeated across forks\nEntropy source failure — security degraded',
        'Control flow integrity violation\nIndirect call to 0x4141414141\nShadow stack mismatch — process terminated',
        'Spectre v2: indirect branch poisoning\nBranch predictor contamination detected\nProc 8812 isolation required — performance hit',
        'Meltdown mitigation disabled: KPTI=off\nKernel mapping exposed in user space\nSystem vulnerable — reboot required',
        'BCI foreign signature: employer key mismatch\nWeapon BIM expects corp_key=0x3301\nLockout enforced — weapon inoperable',
        'BCI motor cortex desync: 12k events lost\nBuffer overflow in neural_rx pipeline\nCalibration state corrupted — reset required',
        'BCI denial-of-sensation attack detected\nInput signal 0xFFFF flooding grip classifier\nOperator sensory loop disrupted — threat',
        'BCI lateral injection: foreign grip pattern\nClassifier trained on 0x3301 profile\nIntent classification unreliable — unsafe',
        'BCI key derivation failed: PBKDF2 abort\nSalt corrupted at byte 32 of 64\nNeural key unrecoverable — factory reset',
        'Corp policy violation: operator ID 3301\nUnauthorized district 4 access attempt\nAll credentials revoked — enforcement alerted',
        'Corp audit log gap: 300 entries missing\nTimestamp 2025-04-10T03:22 to T03:27\nLog tampering suspected — incident open',
        'Corp data exfiltration alert\n4.2MB outbound to 203.0.113.9 port 443\nDestination not in allowlist — blocked',
        'GLMZ enforcement beacon: lockdown active\nAll freelancer IDs suspended district 7\nbci_daemon forced offline by remote cmd',
        'GLMZ dark node exposure: IP 10.44.7.99\nNode appeared in GLMZ topology map\nOperator 3301 location triangulated',
        'GLMZ gate seizure: corp territorial acquisition\nDistrict 4 fabrication license revoked\nAll Crestfall BIM keys invalidated — mass lockout',
        'BCI ring4 contact lost: impedance=INF\nADC channel 4 reading 0 — hardware fault\nCalibration aborted — reattach and retry',
        'BCI ring7 contact fail: impedance=28kΩ\nThreshold exceeded — signal unreliable\nRing replacement required before next session',
        'BCI snr=6dB below minimum threshold\nNoise floor elevated — EM interference?\nClassifier confidence 0.21 — unsafe',
        'BCI waveform corrupt: epoch 4412\nExpected 48Hz motor band — received garbage\nCalibration state invalidated — reset required',
        'BCI model mismatch: expected v3.4 got v2.1\nClassifier weights incompatible with hardware\nFalling back to null mode — weapon safe',
        'BCI operator id mismatch: got 0x7712\nExpected 0x3301 per corp credential store\nSession denied — escalating to sec daemon',
        'BCI neural key verify fail: PBKDF2 mismatch\nSalt at byte offset 16 corrupted\nKey unrecoverable — operator must re-enroll',
        'BCI grip pattern foreign: profile 0x9F not known\nBIM classifier confidence 0.08\nWeapon lockout enforced — threat protocol',
        'BCI intent transition fail: SWEEP→PRECISION\nDisc rotation timeout after 300ms\nHKB impulse correction skipped — unsafe',
        'BCI delta_band 13Hz absent: epoch 4418\nMotor cortex signal not detected\nOperator fatigue? Recalibrate immediately',
        'BCI theta spike: 120μV at ring2\nArtifact flood — epoch discarded\nFiltering engaged — 3 epochs consecutive bad',
        'BCI gamma artifact flood: 800Hz component\nLow-pass filter overwhelmed\nADC saturation suspected — hardware check',
        'BCI beta burst overflow: queue depth=512\nSlot drain falling behind 220ms\nBuffer eviction triggered — 48 events lost',
        'BCI calibration timeout: 120s elapsed\nOperator did not complete sweep/precision cycle\nSession aborted — locked until manual reset',
        'RBS disc rotation timeout: 300ms exceeded\nPort selector stuck at intermediate position\nEmergency stop engaged — both ports locked',
        'RBS port mismatch: bim_cmd=BETA got ALPHA\nSelector position sensor failure\nWeapon discharged in wrong mode — incident',
        'RBS thermal 78C: critical threshold\nDisc bearing lubricant breakdown suspected\nCooldown enforced — 300s lockout active',
        'RBS chamber obstruction detected\nFeed pressure sensor: 0 psi — blockage\nClear procedure required — do not force',
        'RBS led matrix fail: driver 0x3A timeout\nI2C NACK on address 0x48\nStatus indication lost — manual mode required',
        'HKB hydraulic pressure 0 psi\nFluid reservoir empty — sensor confirmed\nSpring-only mode: recoil compensation 40%',
        'HKB piston jam at position 2.8mm\nActuator current 4.2A — stall detected\nCycle aborted — manual extraction required',
        'HKB spring break: coil 3 fractured\nResonance frequency shift +12Hz\nImpulse correction degraded — unsafe',
        'HKB wear_score=0.94: service overdue\nBearing play 0.8mm above tolerance\nOperation suspended pending maintenance',
        'HKB thermal runaway: temp 91C\nFriction coefficient elevated 3x nominal\nEmergency stop — cooldown 600s required',
        'KASAN: use-after-free in bci_classify()\nRead of size 8 at addr ffff888201c4a000\nBuggy address freed at bci_session_end+0x44',
        'KASAN: heap-buffer-overflow in grip_cls()\nWrite of size 4 at addr ffff888103b2c080\nObject bounds exceeded by 4 bytes',
        'KASAN: stack-buffer-overflow in neural_rx()\nWrite of size 64 at addr ffffc9000481be80\nStack frame overflowed — backtrace corrupt',
        'KASAN: null-ptr-deref in bim_calibrate()\nRead of size 8 at addr 0000000000000008\nNull dereference — operator struct not init',
        'KASAN: slab-out-of-bounds in epoch_parse()\nRead of size 16 at addr ffff88810de40200\nOverread by 8 bytes — heap object exhausted',
        'AddressSanitizer: heap-use-after-free\nFree at: bci_session_close+0x22\nUse at: grip_classify+0x8f — UAF confirmed',
        'AddressSanitizer: global-buffer-overflow\nRead at: neural_feature_vec+0x3c\nOffset 2052 of size 2048 — overflow 4B',
        'AddressSanitizer: stack-use-after-return\nVariable at: epoch_handler frame+0x40\nFrame deallocated before use — corruption',
        'UndefinedBehaviorSanitizer: signed overflow\nbci_delta_calc at bci_math.c:88 col:12\nResult: -2147483648 + -1 — undefined',
        'ThreadSanitizer: data race on bim_state\nWrite at: rbs_rotate+0x2a (thread 2)\nRead at: bci_classify+0x5f (thread 1)',
        'TCP RST injected by enforcement node\nconn 10.44.7.3:8443 → 172.16.0.1:443\nActive interception confirmed — abort',
        'TLS handshake fail: cert verify error\nCN=glmz.relay.internal depth=1\nCERT_UNTRUSTED — relay cert rotated?',
        'TLS downgrade to 1.1 attempted\nPeer hello: max_version=TLS1.1\nDowngrade rejected — handshake aborted',
        'DNS resolve fail: glmz-relay.internal\nNXDOMAIN after 3 retries — node gone?\nFailing over to backup 10.44.0.254',
        'DNS poison detected: glmz.relay.internal\nExpected A 10.44.7.3 got 203.0.113.99\nCache flushed — upstream tampered',
        'ARP spoof detected: 10.44.0.1\nExpected MAC aa:bb:cc:dd:ee:ff\nGot 11:22:33:44:55:66 — MITM suspected',
        'BGP hijack detected: prefix 10.44.0.0/16\nOrigin AS 65001 unexpected — not glmz\nAll routes via AS 65001 withdrawn',
        'WireGuard tunnel collapse: peer 10.44.7.3\nHandshake timeout after 5 retries\nRekeying failed — traffic blackholed',
        'Net bridge dist4→dist7 down\nBoth endpoints unreachable\nFalling back to mesh route — 440ms penalty',
        'DDOS detected: 1.2Mpps SYN flood\nSource: spoofed 198.51.100.0/24\nSyn cookies activated — degraded mode',
        'CORP exfil: 4.8MB to 203.0.113.44 port 443\nDestination not in allowlist\nDLP blocked — incident ticket #4412',
        'CORP audit gap: 180 events missing\nTimestamp 2026-04-12T02:14 to T02:17\nLog integrity broken — forensic needed',
        'CORP key rotation fail: vault unreachable\nHSM connection timeout after 30s\nFalling back to cached key — expires in 4h',
        'CORP enforcement lockout: uid=0x3301\nPolicy version 4.2: district 7 denied\nAll credentials suspended — escalated',
        'CORP mirror conflict: node graph delta\n3 edge records diverged from primary\nManual reconciliation required',
        'SEC intrusion: pid=8812 uid=0\nUnexpected root shell from bci_daemon\nProcess isolated — forensic capture started',
        'SEC privilege escalation: uid=1001→0\nSUIDexploit in /usr/bin/pkexec suspected\nCVE-2021-4034 pattern — patch missing',
        'SEC lateral move: 10.44.7.3→10.44.4.11\nRDP brute force 2847 attempts in 60s\nBlocked at enforcer — source blacklisted',
        'SEC rootkit detected: /proc/kallsyms diff\n14 syscall table entries modified\nReboot to clean kernel required',
        'SEC exfil: DNS tunneling detected\nTXT queries 4.4kB avg — abnormal\nDNS resolver rate-limited — tunnel broken',
        'GLMZ gate 44 blacklisted: uid=0x3301\nActive enforcement warrant ID 0x0099\nAll transit points notified — lockdown',
        'GLMZ dark node 10.44.7.99 exposed\nNode appeared in topology scan\nOperator 0x7712 location triangulated d7',
        'GLMZ district 9 lockdown active\nEnforcement beacon 0xBE04 broadcasting\nbci_daemon forced offline — remote cmd',
        'GLMZ corridor B sealed: intrusion alarm\nMotion + thermal sensors triggered simultaneously\nPersonnel containment procedure active',
        'GLMZ relay intercept: traffic redirected\nconn 10.44.7.3:9000 rerouted to 10.44.99.1\nDeep packet inspection active — abort',
        'NullReferenceException in BimCalibrate()\nObject reference not set: calibrationCtx\nStack: BimCalibrate+0x2c BciSession+0x88',
        'NullReferenceException in RbsRotate()\nObject reference not set: discController\nStack: RbsRotate+0x14 WeaponCore+0x4f',
        'IndexOutOfRangeException: ring array\nIndex 8 is out of bounds [0..7]\nStack: RingReader+0x22 BciPoll+0x3a',
        'OverflowException: neural_event_count\nInt32 max exceeded: accumulator=2147483647\nCounter wrap — epoch data invalid',
        'InvalidOperationException: rbs_disc_lock\nDisc locked in transitioning state\nDeadlock between BIM and RBS threads',
        'ObjectDisposedException: bci_session\nSession used after Dispose() called\nUse-after-dispose — ref leak suspected',
        'StackOverflowException: neural_classify\nRecursion depth 10000 exceeded\nbci_daemon pid=8812 terminated — restart',
        'OutOfMemoryException: feature_buffer\nFailed to allocate 67108864 bytes\nHeap pressure — GC unable to collect',
        'CryptographicException: AES-GCM tag fail\nAuthentication tag mismatch — tampered?\nMessage discarded — connection reset',
        'IOException: wal_write fd=7\nerrno=28 ENOSPC — disk full\nDatabase suspended — all writes blocked',
        'Segfault in libneural.so.3+0x4f3a\nAddress 0x00000000000000a8 not mapped\nCore dumped to /tmp/core.8812',
        'Segfault in librbs.so.2+0x2c11\nAddress 0x00000000000000b8 not mapped\nCore dumped to /tmp/core.9144',
        'Segfault in libhkb.so.1+0x1a44\nNull pointer deref in HkbCycle()\nCore dumped to /tmp/core.3301',
        'Double free in tcache bin 3\nChunk 0x5582c4a0 freed twice\nHeap integrity check failed — abort',
        'Stack smashing in neural_rx_handler()\n__stack_chk_fail: canary corrupted\nAbort trap 6 — pid=8812',
        'Stack smashing in grip_cls_loop()\n__stack_chk_fail: canary corrupted\nAbort trap 6 — pid=9144',
        'Heap corruption: free list poisoned\nFast bin fd pointer 0x5582c4b0 invalid\nAbort — heap integrity check failed',
        'EXCEPTION_ACCESS_VIOLATION 0xc0000005\nWrite to 0xffffffffffffffff\nModule: bim_core.dll +0x3a88',
        'EXCEPTION_STACK_OVERFLOW 0xc00000fd\nThread stack exhausted\nModule: neural_rx.dll at 0x7ff8+0x1234',
        'EXCEPTION_ILLEGAL_INSTRUCTION 0xc000001d\nUnknown opcode at 0x7ff8bim+0x88\nPossible code injection detected',
        'Kernel oops #5: BUG at bci_mm.c:1402\nKernel BUG — write to read-only page\nIP: bci_mmap_fault+0x44 CR2: 0x5582c400',
        'Kernel oops #11: bad page state\nPFN 0x1a440 flags 0x00000000\nIP: free_pages_check+0x2c',
        'Kernel oops #14: page fault\nCR2: 0x00000000000000a8\nIP: bci_classify+0x3f PGD 0 P4D 0',
        'Kernel NMI: hardware error\nMCE bank 0: MCGSTATUS=7\nMCI_STATUS=0xbe00000000000800',
        'Kernel MCE: uncorrectable DRAM ECC\nBank 1: addr=0x1a4400000 misc=0x88\nMemory offline — degraded operation',
        'Kernel watchdog: CPU0 hard lockup\nRIP: _raw_spin_lock+0x28\nHeld lock: bci_spinlock — deadlock?',
        'Kernel RCU stall: CPU 2\nrcu_sched kthread starved for 21s\nAll RCU callbacks delayed — system degraded',
        'Kernel hung task: bci_flusher\nTask blocked for 122s on semaphore\nCall trace: bci_flush_wait+0x3c',
        'OOM killer: kill process 8812 (bci_daemon)\nom_score_adj=0 anon-rss=1932M\nOut of memory: force killed',
        'OOM killer: kill process 9144 (neural_rx)\noom_score_adj=500 anon-rss=440M\nSystem memory critically low',
        'Meltdown: KPTI disabled — kernel exposed\nSpectre v2: IBRS not active\nSystem vulnerable — patch not applied',
        'Spectre-v1 gadget in bci_classify()\nArray index bounds not enforced\nSpeculative load gadget at +0x3c',
        'Spectre-v4: store bypass possible\nbci_session_ctx speculative read\nSSBD not enabled — memory poisoning risk',
        'CORP ransomware: mass encryption active\n44 files encrypted in 3s — CryptoLocker variant\nIsolate and restore from backup',
        'CORP apt: persistent backdoor installed\nRegistry RunKey bci_svc added\nC2 beacon to 203.0.113.99:443 detected',
        'CORP zero-day: CVE-2026-XXXX active\nLibneural.so heap spray at +0x3a00\nPatch not available — compensating control',
        'CORP supply chain: package tamper\nlibneural.so.3 checksum mismatch\nExpected sha256 a1b2c3 got 99ff00 — abort',
        'CORP insider threat: bulk download\nOperator 0x3301 downloaded 2.4GB in 8m\nDLP alert — session suspended pending review',
        'DB integrity check failed: page 4412\nChecksum mismatch — expected 0x3a44 got 0xff00\nTable bci_events corruption confirmed',
        'DB wal overflow: segment 88 full\nWAL size exceeded 1GB limit\nCheckpoint forced — writes blocked 30s',
        'DB deadlock: txn 3301 and txn 7712\nCycle: txn3301 waits for txn7712\ntxn7712 waits for txn3301 — victim chosen',
        'DB replication lag: 8.4s behind primary\nNetwork throughput 2.1MB/s vs 12MB/s needed\nReplica paused — failover risk',
        'DB snapshot corrupt: epoch 88\nHeader magic 0xDEADBEEF unexpected\nFalling back to WAL replay from epoch 44',
        'DB injection attempt: table bci_events\nInput: 1 OR 1=1 -- detected\nQuery aborted — audit log written',
        'DB disk full: /var/db/bci 0B free\nWrites suspended — emergency cleanup\nPurge archives > 30d to free space',
        'DB connection pool exhausted: max=64\nAll connections busy — queue depth 128\nNew requests rejected — backpressure active',
        'FS journal abort: block device error\nerrno=EIO on /dev/sda1 at sector 441200\nFilesystem mounted read-only — repairs needed',
        'FS inode table corrupt: group 12\nInode 44120 magic number invalid\nfsck required before remount',
        'FS dirty pages overflow: 65536 pending\nFlusher thread behind 22s\nKernel may panic — emergency sync',
        'FS quota exceeded: uid=1001\n20GB hard limit reached — 0B free\nAll writes rejected until purge',
        'FS bad block: /dev/sdb1 sector 441200\nRead EIO — sector marked bad\nFilesystem degraded — backup immediately',
        'MEM guard page violation: addr 0x7fff4000\nWrite to non-writable guard page\nStack overflow suspected — abort',
        'MEM pkey violation: pkey=3 addr 0x5582c000\nAccess not permitted for current pkey\nSegfault — process terminated',
        'MEM asan report: heap-buffer-overflow\nWrite of 4 past end of 2048B buffer\nStack: bci_ring_write+0x3a grip_cls+0x88',
        'MEM ubsan: integer overflow at bci_math.c:44\nSigned overflow: 2147483647 + 1\nUndefined behavior — value unreliable',
        'MEM leak detected: 44 objects unreachable\nTotal leaked: 1.2MB in 120s\nLeaking from bci_session_alloc+0x22',
        'CRYPT AES-GCM tag verify fail\nExpected tag a1b2c3d4e5f67890\nGot tag 0000000000000000 — tampered packet',
        'CRYPT ECDH derive fail: invalid point\nPeer public key not on curve P-256\nPossible attack — connection aborted',
        'CRYPT HMAC mismatch: neural telemetry\nExpected HMAC a1b2c3 got d4e5f6\nMessage integrity violated — abort',
        'CRYPT RNG entropy low: 4 bits available\n/dev/random exhausted — blocking\nKey generation paused — entropy needed',
        'CRYPT nonce wrap imminent: counter=2^32-8\nChacha20 nonce exhaustion in 8 messages\nRekeying required immediately',
        'CRYPT PBKDF2 fail: salt corrupt at byte 32\nDerived key all-zero — unusable\nNeural key unrecoverable — factory reset',
        'CRYPT ed25519 verify fail: sig mismatch\nPublic key 0x3301 signature invalid\nPossible forgery — session rejected',
        'CRYPT RSA decrypt fail: padding error\nPKCS#1 v1.5 padding invalid\nOracle attack possible — switch to OAEP',
        'CRYPT GCM nonce reuse detected\nSame nonce used for 2 messages\nKey stream exposed — immediate rekey',
        'CRYPT argon2id fail: memory 65536KB\nOut of memory during KDF\nKey derivation aborted — retry with less mem',
        'ARC integrity fail: sha256 mismatch\nExpected a1b2c3d4 got 99ff0011\nArchive corrupted — restore from backup',
        'ARC delta decode fail: base missing\nBase hash 0x3a44 not in catalog\nFull snapshot required — incremental broken',
        'ARC index corrupt: entry 4412\nMagic byte 0xDEAD unexpected\nCatalog rebuild required — catalog.json lost',
        'ARC checksum fail: block 88 lz4\nDecompressed hash mismatch\nBlock corrupt — cannot decompress safely',
        'ARC rollback fail: target epoch 44\nSnapshot not found in cold storage\nData loss risk — manual recovery needed',
        'JIT compile fail: depth 8 exceeded\nInlining budget exceeded at call +0x88\nFalling back to interpreter — 10x slower',
        'JIT deopt guard miss: assumption violated\nType check failed at bim_classify+0x3c\nDeopt triggered — recompile queued',
        'JIT code cache full: 64MB exhausted\nEviction failed — no cold entries\nCompilation suspended — interpreter mode',
        'JIT illegal instruction generated\nOpcode 0x0F0B (UD2) at trace+0x44\nJIT bug — process terminated',
        'JIT trace abort: infinite loop detected\nBack edge count 100000 exceeded\nTrace compilation cancelled',
        'IPC deadlock: sem 0 and sem 1\nbci_thread holds sem0 waits sem1\nrbs_thread holds sem1 waits sem0',
        'IPC pipe broken: SIGPIPE pid=8812\nRead end closed before write\nWriter terminated — data lost',
        'IPC shared mem corrupt: key=0x3301\nMagic header invalid\nAttach rejected — reinitialize required',
        'IPC message queue overflow: qid=4\nQueue depth 1024 full — messages lost\nConsumer too slow — backpressure needed',
        'IPC socket connection reset: fd=7\nPeer closed unexpectedly\nIn-flight data lost — retry required',
        'SCHED deadlock: cpu0 and cpu1 locked\nBoth waiting on cross-cpu lock\nWatchdog triggered — reboot forced',
        'SCHED starvation: pid=3301 bci_daemon\n480s without CPU time\nCgroup throttle too aggressive — tune',
        'SCHED latency 220ms: bci_classify\nDeadline missed — grip decision delayed\nWeapon mode indeterminate — abort',
        'SCHED cgroup limit: neural_rx 100ms\nBurst consumed — throttled to 50ms\nEvent processing 3x slower',
        'SCHED cpu lockup: CPU3 hard lockup\nRIP: bci_spinlock+0x18\nNMI triggered — core offline',
        'PCI DMA error: dev 0:1b.0\nDMA read timeout — IOMMU fault\nDevice reset required — PCIe link degraded',
        'USB transfer timeout: dev 3-1.2 ep 0x81\nTransfer timeout after 5s\nDevice disconnected — reconnect required',
        'HW temperature 91C: cpu0 throttled\nFrequency reduced to 800MHz\nbci_daemon latency 10x nominal',
        'HW i2c NACK: addr 0x48 (rbs led)\nNo acknowledgement after 3 retries\nLED matrix offline — status indication lost',
        'HW uart overflow: rx fifo full\nIncoming data rate 2Mbps > 115200\nData lost — baud rate mismatch',
        'BCI ring4 contact lost: ADC=0 impedance=INF\nElectrode gel dried — hardware fault\nCalibration aborted — session terminated',
        'BCI neural_rx overflow: queue=4096 full\n220 events dropped in last 100ms\nClassifier state stale — weapon locked',
        'BCI epoch 4419 desync: timestamp gap +3.2s\nExpected delta 20ms got 3200ms\nSession continuity broken — reauth needed',
        'BCI foreign injection: pattern 0x9F12 recv\nUnknown operator profile in BIM\nSecurity lockout engaged — incident #4412',
        'BCI motor cortex signal null: ring 0-3 zero\nAll four rings reading baseline\nGlove sensor array offline — check conn',
        'GLMZ enforcement warrant: ID 0x3301\nActive warrant for unauthorized d4 entry\nAll transit nodes alerted — capture order',
        'GLMZ operator triangulated: 0x7712\nThree beacon bearings intersect d7-C4\nLocation fix 8m accuracy — team dispatched',
        'GLMZ comms intercept: relay 10.44.0.99\nTraffic redirected to inspection node\nAll plaintext data captured — abort',
        'GLMZ zone B lockdown: intrusion alarm\nThree simultaneous sensor triggers\nPersonnel immobilization protocol active',
        'GLMZ cam network offline: district 4\n14 of 16 cameras unreachable\nSabotage suspected — backup feeds only',
        'DB graph cycle detected: node 3301\nPath: 3301→7712→4492→3301\nCycle breaks topological ordering — abort',
        'DB foreign key violation: edge table\nReferenced node 0x3301 deleted\nCascade blocked — orphan edges remain',
        'DB type mismatch: column epoch INT64\nGot string "4412a" — parse error\nRow rejected — insert aborted',
        'DB lock timeout: txn 8812 waited 30s\nLock held by txn 9144 uncommitted\nTimeout abort — retry required',
        'DB vacuum abort: disk I/O error\nPage 4412 read EIO — sda1 fault\nVacuum incomplete — fragmentation grows',
        'NET beacon spoof: enf node 0xBE04\nRogue beacon transmitting on freq 915MHz\nFreelancers being lured — avoid district 7',
        'NET covert channel: icmp data payload\n1400B ICMP echo with encrypted payload\nExfil tunnel — rate limited + logged',
        'NET traffic analysis: timing correlation\nPacket intervals match known C2 pattern\nEnforcement correlation active',
        'NET rate limit exceeded: 10.44.7.3\n1000 req/s threshold breached\nConnection throttled — 429 responses',
        'NET reflection attack: DNS amplification\n0.1Mbps sent → 4.4Gbps reflected\nUpstream nullroute applied',
        'SEC hypervisor escape attempt\nCVE-2026-XXXX: VM escape via MMIO\nHypervisor patched — vm restarted',
        'SEC container escape: pid=9144\nCgroup namespace breakout detected\nContainer isolated — host processes safe',
        'SEC bootkit: MBR sector 0 modified\nExpected hash a1b2c3 got ff0099\nBootloader compromised — reinstall',
        'SEC firmware tamper: bci_daemon flash\nFlash region 0x10000 checksum mismatch\nFirmware rollback to v3.2 — audit open',
        'SEC golden ticket: kerberos forged\nTGT for uid=0 valid 10 years\nKDC secret compromised — rotate immediately',
        'CORP zero-day CVE-2026-0041: active\nRemote code exec in net_daemon v4.1\nPatch deploying — interim firewall rule',
        'CORP ransomware: 128 files encrypted\nExtension .glock appended — known strain\nIsolate host — restore from offline backup',
        'CORP apt beacon: C2 to 198.51.100.44\nBeacon interval 300s — Cobalt Strike?\nC2 blocked — hunt for lateral movement',
        'CORP data destruction: bci_logs wiped\n/var/log/bci/*.log deleted at 03:22\nAnti-forensic activity — incident open',
        'CORP insider threat: 0x3301 screenshot\nScreen capture software installed covertly\nDLP alert — session terminated',
        'ARC cold storage fail: bucket unavailable\nS3 endpoint timeout after 30s\nArchival suspended — retry in 15m',
        'ARC manifest verify fail: entry 88\nSHA-256 expected a1b2 got c3d4\nManifest tampered — restore from replica',
        'ARC expiry purge fail: permission denied\nProcess uid=1001 cannot delete uid=0 files\nSudo required — manual intervention',
        'ARC dedup collision: hash a1b2c3d4\nTwo distinct 4K blocks with same hash\nSHA-256 collision? — verify algorithm',
        'ARC stream abort: connection reset mid-xfer\nReceived 2.1MB of expected 4.4MB\nPartial archive — resume required',
        'JIT profile corrupt: hotspot table\nProfile header magic invalid 0xFF00\nProfile discarded — cold compilation mode',
        'JIT osr entry fail: backedge at +0x88\nOSR not possible — frame layout mismatch\nFalling back to baseline interpreter',
        'JIT regalloc spill overflow: 64 spill slots\nRegister pressure too high\nCompilation aborted — no space on stack',
        'JIT constant fold loop: infinite iteration\nFolder detected cycle in const graph\nCompilation aborted — graph corrupt',
        'JIT branch prediction miss 88%: hot loop\nBranch pattern non-predictable\nPGO data insufficient — retrain profile',
        'IPC futex contention: 1200 waiters\nbci_lock contended by 1200 threads\nSystem latency 800ms — livelock risk',
        'IPC condvar spurious wakeup storm\n4000 spurious wakeups in 10s\nCondition variable misuse — busy loop',
        'IPC spinlock CPU burn: cpu0 100%\nSpinning for 400ms — no progress\nDeadlock suspected — NMI triggered',
        'IPC rwlock writer starvation\nWriter blocked by continuous readers\n88 seconds without write — livelock',
        'IPC barrier timeout: 8 of 16 arrived\nRemaining 8 threads hung — crashed?\nBarrier abandoned — results invalid',
        'SCHED SIGXCPU: bci_daemon 300s CPU\nProcess consumed 300s wall CPU\nKilled — CPU quota exceeded',
        'SCHED workqueue flood: 65536 pending\nBCI event workqueue overflow\nDropping oldest events — data loss',
        'SCHED kthread stall: bci_flusher\nKernel thread blocked 120s\nForce stopped — filesystem at risk',
        'SCHED irq storm: irq 44 cpu0 100%\nBCI ADC interrupt rate 2MHz\nDriver rate limiting broken — system hang',
        'SCHED preemption disabled too long: 88ms\nbci_spinlock held across schedule\nLatency anomaly — fix critical section',
        'MEM kasan: out-of-bounds in epoch_buf\nWrite at offset 4096 of 4096B obj\nLast byte overwrite — off-by-one',
        'MEM oom: bci_feature_cache 1.9GB\nCache not bounded — unbounded growth\nOOM kill at 1.9GB — bound cache',
        'MEM huge page alloc fail: ENOMEM\nHugeTLB pool exhausted 0 pages free\nFalling back to 4K pages — TLB pressure',
        'MEM pkey fault: key=2 write at 0x5582c000\nProtection key violation on write\nProcess terminated — exploit attempt?',
        'MEM stack smash: bci_classify frame\nCanary 0x12345678 overwritten with 0x41414141\nReturn address corrupted — exploit?',
        'SEC dcsync: ldap query for krbtgt\nOperator 0x3301 querying AD password hashes\nDomain controller alert — contain',
        'SEC pass-the-hash: ntlm auth from 3301\nHash reuse from compromised credential\nLateral movement blocked — rotate',
        'SEC kerberoast: SPN enum by 0x7712\n12 service tickets requested in 3s\nOffline cracking suspected — rotate SPNs',
        'SEC process injection: pid=8812→9144\nWriteProcessMemory + CreateRemoteThread\nCode injected — isolate both processes',
        'SEC credential dump: lsass accessed\nOpenProcess + ReadProcessMemory on lsass\nPass-the-hash risk — rotate all creds',
        'KERN page fault #14 in bci_classify()\nCR2: 0x00000000000000b0 — null deref\nRIP: bci_classify+0x3a4 CS: 0x10',
        'KERN machine check: uncorrectable mem\nMCI_STATUS: 0xbe00000000000800\nMemory offline — degraded DRAM ECC',
        'KERN IRQ 44 affinity storm\nAll CPUs handling bci ADC interrupts\nAffinity pinning broken — rebalance',
        'KERN soft lockup: CPU1 22s\nTask bci_classify not scheduling\nWatchdog fired — process killed',
        'KERN IOMMU fault: dev 0:1b.0\nDMA to address 0x00000000 — null\nbci ADC driver null pointer bug',
        'FS ext4 error: block group 12 bitmap\nBad block count — filesystem inconsistent\numount and fsck required',
        'FS journal commit blocked: EIO\n/dev/sdb1 sector 441200 write error\nJournal stalled — filesystem readonly',
        'FS inode leak: 4412 inodes unclaimed\nOpen but unlinked files not closed\nFD leak in bci_session — close on exit',
        'FS xattr corrupt: security.capability\nCapability xattr decode fail\nProcess drops capabilities — broken env',
        'FS sync timeout: 30s dirty pages\nFlusher blocked on device write\nKernel may force reboot — alert ops',
        'HKB emergency stop: temp 94C\nThermal sensor trip at 90C threshold\nWeapon cold-locked — 600s cooldown',
        'HKB spring fail: resonance +18Hz shift\nFrequency analyzer: spring 3 coil break\nImpulse compensation 60% degraded',
        'HKB piston stall: current 4.4A\nMotor controller fault code 0xE3\nManual extraction required — do not fire',
        'HKB fluid sensor: 0mL remaining\nReservoir empty — 3 cycles since warning\nHydraulic mode disabled — spring only',
        'HKB actuator fault: code 0xA7\nActuator controller CAN bus timeout\nPiston position unknown — emergency stop',
        'RBS selector corrupt: position=2 undefined\nExpected 0 (alpha) or 1 (beta)\nDisc in undefined position — abort',
        'RBS thermal trip: 78C bearing temp\nLubricant breakdown — friction 3x\nMotor current 4.1A — stall imminent',
        'RBS jam: foreign object detected\nFeed mechanism blocked at position 3\nClear procedure: release pin + extract',
        'RBS misfire: primer struck, no discharge\nRound seated but no pressure wave\nDud round — extract and inspect',
        'RBS discharge blocked: safety override\nSafety engaged remotely by corp policy\nDischarge disallowed in zone B',
        'DB orphan edges: 412 unreachable nodes\nGarbage collect failed — nodes pinned\nManual vacuum required — db.Sweep()',
        'DB query timeout: 44s on graph traverse\nPath query depth=12 nodes=9871\nQuery plan chose full scan — index hint',
        'DB unique violation: bci_event.epoch_id\nEpoch 4412 already inserted\nDuplicate insert rejected — idempotency bug',
        'DB graph path fail: node 0x3301 missing\nNode deleted but edges remain\nReferential integrity broken',
        'DB stats stale: 180s since last update\nQuery planner using 6h old statistics\nForce ANALYZE — plans suboptimal',
        // 30 more
        'Neural bridge dropout: ring4 lost contact\nEEG amplitude < noise floor 18ms\nBCI handshake reset — reclassify',
        'BCI handshake refused: key 0xBE04 revoked\nVault reports key expired 48h ago\nOperator locked out — manual unlock',
        'RBS spring shatter: resonance sensor 0x3A\nHigh-speed impact fragment at 38ms\nDisc rotation halted — catastrophic fail',
        'HKB valve blowout: pressure 140 PSI\nSeat gasket failed — rated 110 PSI\nVent to atmosphere — gas leak active',
        'GLMZ gate 0x22 auth fail uid=0x4492\nCert revoked by enforcement order 9412\nAll routes via gate blocked — reroute',
        'Relay blackhole: dist7→dist12 loop\nTTL expired 3 hops — routing cycle\nPacket black-holed — trace route',
        'Key store corrupt: sector 0x44A2\nHMAC mismatch on master record\nKey material unreadable — vault sealed',
        'DB redo log corrupt: frame 8192\nChecksum 0xDEAD != expected 0x3AF7\nRecovery impossible — restore from backup',
        'VM page table fault: cr3=0x0044 rip=0x00\nKernel null-pointer deref in vm_alloc\nSystem halted — memory model broken',
        'JIT segment overflow: code cache full\nCache 4MB — eviction threshold hit 100%\nHot functions deoptimised — degraded',
        'IPC ring overflow: consumers stalled\nProducer pushed 4096 unread messages\nRing sealed — bci_daemon blocked',
        'TLS session bomb: 0 bytes handshake\nClient sent RST mid-ClientHello\nMitM or scan probe — block IP',
        'BCI flood attack: 14kpps to ring buffer\nMalformed ADC frames from addr 0xBE04\nBuffer overflow risk — drop source',
        'RBS cam desync: encoder mismatch 0x22\nEncoder A and B disagree by 3 ticks\nPosition unknown — emergency stop',
        'HKB rack jam: gear 3 stall at 4ms\nFeed mechanism seized mid-stroke\nDo not fire — mechanical hazard',
        'GLMZ perimeter breach: dist4 wall C\nForce sensor 0x3301 tripped, 0xBE04 quiet\nEnforcement notified — 90s response ETA',
        'Corp exfil volume: 48GB to 203.0.113.9\nDestination corp blacklist match rate 94%\nDLP blocked session — forensic queued',
        'SEC replay storm: 8800 tokens in 30s\nSame nonce 0x3301 repeated 4400×\nSession revoked — incident #8812',
        'DB WAL storm: 4GB redo in 22s\nWrite rate 200MB/s — disk saturated\nCheckpoint blocked — db frozen',
        'ARP poison: gateway 10.44.0.1 spoofed\nMAC 0:0:0:9f:3a:01 claims GW address\nTraffic intercepted — network isolated',
        'VM OOM cascade: 8 processes SIGKILL\nFree pages: 0 — kernel killing tasks\nSystem degraded — restart required',
        'JIT cache corrupt: hash mismatch fn=0x44\nExpected bytes [90 90 90] got [cc cc cc]\nExecution halted — possible patch',
        'IPC deadlock global: all queues stalled\nbci_daemon, rbs_ctrl, hkb_mon all waiting\nSystem heartbeat timeout — restart',
        'Archive block missing: idx entry 0x3AF7\nBlock referenced by 3 nodes, file deleted\nIntegrity broken — rebuild index',
        'Slab poison detect: kmem cache bci_buf\nPoison pattern 0x6b overwritten\nuse-after-free confirmed — halt',
        'HPET timer fault: counter frozen 44ms\nHardware timer register read returns 0\nSystem timekeeping degraded',
        'CPUID mismatch: hypervisor flag set\nNative boot expected — VM detected\nEnclave attestation invalidated — abort',
        'Auth module unresponsive: 10s timeout\nCrest auth IPC no reply to 8 queries\nWeapon auth state unknown — safe mode',
        'District feed blackout: dist4→GLMZ\nNo telemetry for 180s — correlation lost\nBlind spot in enforcement coverage',
        'BCI classifier poison inject\nAdversarial signal injected via ring2\nClassifier predicting adversary labels',
    ];

    // ── Floating code fragments ─────────────────────────────────────────────
    var FRAGS = [
        'for (int i = 0; i < n; i++) {\n    if (buf[i] == null) continue;\n    for (int j = 0; j < buf[i].Length; j++) {\n        if (!validator.Check(buf[i][j])) {\n            log.Warn($"invalid at [{i},{j}]");\n            fault.Record(i, j);\n            continue;\n        }\n        dispatch.Enqueue(buf[i][j]);\n    }\n    if (fault.Count > MAX_FAULTS) {\n        throw new IntegrityException(i);\n    }\n}',
        'while (!queue.IsEmpty()) {\n    var task = queue.Dequeue();\n    if (task.Priority == Priority.High) {\n        for (int r = 0; r < MAX_RETRY; r++) {\n            try {\n                await task.RunAsync(cts.Token);\n                break;\n            } catch (TimeoutException) {\n                if (r == MAX_RETRY - 1) throw;\n                await Task.Delay(backoff[r]);\n            }\n        }\n    } else {\n        pool.Submit(task);\n    }\n}',
        'switch (bci.IntentState) {\n    case IntentState.Precision:\n        if (rbs.CurrentPort != Port.Beta) {\n            await rbs.RotateAsync(Port.Beta);\n            led.Set(LedColor.Blue);\n            pin.Extend();\n        }\n        break;\n    case IntentState.Sweep:\n        if (rbs.CurrentPort != Port.Alpha) {\n            await rbs.RotateAsync(Port.Alpha);\n            led.Set(LedColor.Red);\n            pin.Retract();\n        }\n        break;\n    case IntentState.Transitioning:\n        led.Set(LedColor.Yellow);\n        await Task.Delay(RBS_ROTATION_MS);\n        break;\n    default:\n        if (mode != OverrideMode.Manual)\n            weapon.SafetyEngage();\n        break;\n}',
        'Task.Run(async () => {\n    while (!cts.IsCancellationRequested) {\n        for (int ring = 0; ring < CONTACT_RINGS; ring++) {\n            raw[ring] = adc.Sample(ring);\n            delta[ring] = raw[ring] - baseline[ring];\n            if (Math.Abs(delta[ring]) > NOISE_FLOOR) {\n                features[ring] = delta[ring] / baseline[ring];\n            } else {\n                features[ring] = 0f;\n            }\n        }\n        var intent = bim.Classify(features);\n        if (intent != lastIntent) {\n            rbs.RequestMode(intent);\n            lastIntent = intent;\n        }\n        await Task.Delay(POLL_INTERVAL_MS);\n    }\n});',
        'for (int epoch = 0; epoch < MAX_EPOCHS; epoch++) {\n    float acc = 0f;\n    for (int s = 0; s < samples.Count; s++) {\n        var pred = bim.Infer(samples[s].Features);\n        if (pred == samples[s].Label) acc++;\n    }\n    acc /= samples.Count;\n    if (acc >= TARGET_ACCURACY) {\n        bim.Commit();\n        log.Info($"cal done epoch={epoch} acc={acc:P1}");\n        break;\n    }\n    if (epoch == MAX_EPOCHS - 1) {\n        log.Warn("cal incomplete — defaulting");\n        bim.Revert();\n    }\n    bim.Adjust(lr * (1f - acc));\n}',
        'do {\n    var sig = cortex.Poll(POLL_TIMEOUT);\n    if (sig == null) { missedPolls++; continue; }\n    bim.Feed(sig);\n    if (bim.IsLocked) {\n        for (int i = 0; i < VERIFY_PASSES; i++) {\n            if (!bim.Verify(sig)) {\n                bim.Unlock();\n                missedPolls++;\n                break;\n            }\n        }\n    }\n    if (missedPolls > MAX_MISSED) {\n        log.Fatal("cortex signal lost");\n        throw new NeuralDesyncException();\n    }\n} while (!bim.IsLocked || missedPolls > 0);',
        'foreach (var node in graph.Nodes\n    .Where(n => n.District == targetDistrict)\n    .OrderBy(n => n.Latency)) {\n    if (node.IsBlacklisted) continue;\n    try {\n        var resp = await relay.PingAsync(node, PING_TIMEOUT);\n        if (resp.RTT < bestRTT) {\n            bestRTT  = resp.RTT;\n            bestNode = node;\n        }\n    } catch (TimeoutException) {\n        node.MissCount++;\n        if (node.MissCount > MAX_MISS) {\n            node.IsBlacklisted = true;\n            log.Warn($"node {node.Id} blacklisted");\n        }\n    }\n}',
        'for (int x = 0; x < W; x++) {\n    for (int y = 0; y < H; y++) {\n        float dx = Math.Min(x / (W * EDGE_FRAC), 1f);\n        float dy = Math.Min(y / (H * EDGE_FRAC), 1f);\n        float edge = Math.Min(dx, dy);\n        float n = fbm(\n            x / (float)W * NOISE_FREQ,\n            y / (float)H * NOISE_FREQ,\n            NOISE_OCTAVES);\n        float lo = n * TEAR_SCALE;\n        alpha[x, y] = Smoothstep(lo, lo + FEATHER, edge);\n        pixels[(y * W + x) * 4 + 3] =\n            (byte)(pixels[(y * W + x) * 4 + 3] * alpha[x, y]);\n    }\n}',
        'while (rbs.IsMoving) {\n    if (++elapsed > RBS_TIMEOUT_MS) {\n        rbs.EmergencyStop();\n        led.Set(LedColor.Dark);\n        switch (failMode) {\n            case FailMode.LockAlpha:\n                port = Port.Alpha;\n                break;\n            case FailMode.LockBeta:\n                port = Port.Beta;\n                break;\n            default:\n                weapon.SafetyEngage();\n                throw new RbsJamException(elapsed);\n        }\n        break;\n    }\n    await Task.Delay(1);\n}',
        'if (!enclave.Attest(expectedPcr)) {\n    log.Fatal("enclave attestation failed");\n    foreach (var key in vault.Keys.ToList()) {\n        try {\n            key.Revoke();\n            audit.Record(AuditEvent.KeyRevoked, key.Id);\n        } catch (Exception ex) {\n            log.Error($"revoke failed {key.Id}: {ex.Message}");\n        }\n    }\n    if (!lockdown.IsActive) {\n        lockdown.Engage(LockdownLevel.Hard);\n        await Task.Delay(LOCKDOWN_DELAY_MS);\n        net.Isolate();\n    }\n    Environment.Exit(ENCLAVE_FAIL_CODE);\n}',
        'Task.Run(async () => {\n    int failures = 0;\n    while (!cts.IsCancellationRequested) {\n        try {\n            await heartbeat.SendAsync(cts.Token);\n            failures = 0;\n        } catch (OperationCanceledException) {\n            break;\n        } catch (Exception ex) {\n            failures++;\n            log.Warn($"heartbeat fail #{failures}: {ex.Message}");\n            if (failures >= MAX_HB_FAILURES) {\n                log.Fatal("heartbeat dead — isolation");\n                net.Isolate();\n                return;\n            }\n        }\n        await Task.Delay(\n            failures == 0 ? INTERVAL_MS : INTERVAL_MS * (1 << failures),\n            cts.Token);\n    }\n});',
        'for (int page = 0; page < totalPages; page++) {\n    var buf = fs.ReadPage(page);\n    var actual   = Crc32.Compute(buf);\n    var expected = checksums[page];\n    if (actual != expected) {\n        corruptPages.Add(page);\n        log.Error($"page {page}: crc {actual:X8} != {expected:X8}");\n        if (corruptPages.Count > MAX_CORRUPT) {\n            throw new FilesystemCorruptException(\n                $"{corruptPages.Count} corrupt pages");\n        }\n        var recovered = journal.Recover(page);\n        if (recovered != null) {\n            fs.WritePage(page, recovered);\n            log.Info($"page {page} recovered from journal");\n        }\n    }\n}',
        'switch (pkType) {\n    case KeyType.Rsa4096:\n        if (!rsa.Verify(data, sig, cert.PublicKey)) {\n            throw new SignatureException("RSA-4096 invalid");\n        }\n        break;\n    case KeyType.EcdsaP256:\n        if (!ec.Verify(data, sig, cert.PublicKey)) {\n            throw new SignatureException("ECDSA-P256 invalid");\n        }\n        break;\n    case KeyType.Ed25519:\n        if (!ed.Verify(data, sig, cert.PublicKey)) {\n            throw new SignatureException("Ed25519 invalid");\n        }\n        break;\n    default:\n        audit.Alert(AuditEvent.UnknownKeyType, pkType);\n        throw new UnknownKeyTypeException(pkType);\n}',
        'while (wal.HasUnflushedFrames) {\n    var batch = wal.ReadBatch(CHECKPOINT_BATCH);\n    for (int i = 0; i < batch.Length; i++) {\n        db.ApplyFrame(batch[i]);\n        if (!db.VerifyFrame(batch[i])) {\n            log.Fatal($"WAL frame {batch[i].Lsn} corrupt");\n            db.RollbackTo(lastGoodLsn);\n            throw new WalCorruptException(batch[i].Lsn);\n        }\n        lastGoodLsn = batch[i].Lsn;\n    }\n    if (wal.FrameCount % CHECKPOINT_INTERVAL == 0) {\n        await wal.CheckpointAsync();\n        log.Debug($"checkpoint lsn={lastGoodLsn:X}");\n    }\n}',
        'foreach (var hop in route.Hops) {\n    if (enforcement.IsBlacklisted(hop.District)) {\n        log.Warn($"hop district {hop.District} blacklisted");\n        var alt = router.FindAlternate(hop, route.Destination);\n        if (alt == null) {\n            throw new NoRouteException(route.Destination);\n        }\n        route.Replace(hop, alt);\n        continue;\n    }\n    for (int attempt = 0; attempt < FORWARD_RETRIES; attempt++) {\n        if (relay.Forward(packet, hop)) break;\n        if (attempt == FORWARD_RETRIES - 1) {\n            hop.FailCount++;\n            route.Remove(hop);\n        }\n        await Task.Delay(RETRY_DELAY_MS << attempt);\n    }\n}',
        'do {\n    var chunk = heap.TryAlloc(size, alignment);\n    if (chunk != null) {\n        if (zeroed) chunk.Zero();\n        return chunk;\n    }\n    switch (oomPolicy) {\n        case OomPolicy.Collect:\n            gc.Collect(GcGeneration.All);\n            break;\n        case OomPolicy.Compact:\n            heap.Compact();\n            break;\n        case OomPolicy.Fail:\n            throw new OutOfMemoryException(\n                $"alloc {size}B align={alignment} failed");\n    }\n    retries++;\n} while (retries < MAX_ALLOC_RETRIES);',
        'for (int shard = 0; shard < vault.ShardCount; shard++) {\n    var s = vault.GetShard(shard);\n    if (!s.Verify(masterKey)) {\n        log.Warn($"shard {shard} verify failed — skipping");\n        badShards++;\n        continue;\n    }\n    unlockedShards++;\n    xorBuf.XorWith(s.KeyMaterial);\n    if (unlockedShards >= vault.Threshold) {\n        derivedKey = kdf.Derive(xorBuf, salt, KDF_ITERATIONS);\n        log.Info($"vault unsealed: {unlockedShards}/{vault.ShardCount}");\n        break;\n    }\n}\nif (unlockedShards < vault.Threshold) {\n    throw new VaultSealException(\n        $"only {unlockedShards} of {vault.Threshold} shards");\n}',
        'while (jit.HasPendingWork) {\n    var fn = jit.DequeueHotFunction();\n    if (fn.CallCount < JIT_THRESHOLD) continue;\n    try {\n        var ir  = lifter.Lift(fn.Bytecode);\n        var opt = optimizer.Run(ir, OptLevel.O2);\n        var mc  = emitter.Emit(opt);\n        jit.Install(fn, mc);\n        stats.JitCompiles++;\n    } catch (CompileException ex) {\n        log.Warn($"jit fail {fn.Name}: {ex.Message}");\n        fn.JitBlacklisted = true;\n    }\n    if (jit.CodeCacheUsed > JIT_CACHE_LIMIT) {\n        jit.EvictCold(EVICT_FRACTION);\n        log.Debug($"jit evict: cache={jit.CodeCacheUsed}B");\n    }\n}',
        'Task.Run(async () => {\n    while (!cts.IsCancellationRequested) {\n        sig = Syscall.Poll(fds, POLL_TIMEOUT);\n        if (sig < 0) {\n            if (sig == EINTR) continue;\n            throw new SyscallException("poll", sig);\n        }\n        foreach (var fd in fds.Where(f => f.IsReady)) {\n            switch (fd.Events) {\n                case PollEvent.Read:\n                    var data = fd.Read();\n                    if (data.Length == 0) { fd.Close(); break; }\n                    handler.OnData(fd, data);\n                    break;\n                case PollEvent.HangUp:\n                    handler.OnClose(fd);\n                    fd.Close();\n                    break;\n                case PollEvent.Error:\n                    handler.OnError(fd);\n                    break;\n            }\n        }\n    }\n});',
        'for (int i = 0; i < contactRings; i++) {\n    raw    = adc.Read(i);\n    delta  = raw - baseline[i];\n    if (Math.Abs(delta) <= noiseFl) {\n        features[i] = 0f;\n        continue;\n    }\n    features[i] = delta / (float)baseline[i];\n    if (features[i] > 0 && !seenPositive) {\n        seenPositive   = true;\n        precisionScore += features[i];\n    } else if (features[i] < 0) {\n        sweepScore -= features[i];\n    }\n}\nvar result =\n    precisionScore > sweepScore + HYSTERESIS\n        ? GripIntent.Precision\n        : sweepScore > precisionScore + HYSTERESIS\n            ? GripIntent.Sweep\n            : lastIntent;',
        'while (relay.BackoffMs < MAX_BACKOFF_MS) {\n    try {\n        var conn = await relay.ConnectAsync(\n            dist12, relay.BackoffMs, cts.Token);\n        if (conn.IsAuthenticated) {\n            log.Info($"relay up latency={conn.RTT}ms");\n            relay.BackoffMs = BASE_BACKOFF_MS;\n            return conn;\n        }\n        log.Warn("relay auth rejected — backoff");\n    } catch (OperationCanceledException) {\n        throw;\n    } catch (Exception ex) {\n        log.Warn($"relay fail: {ex.Message}");\n    }\n    relay.BackoffMs = Math.Min(\n        relay.BackoffMs * 2, MAX_BACKOFF_MS);\n    await Task.Delay(relay.BackoffMs, cts.Token);\n}\nthrow new RelayUnreachableException(dist12);',
        'if (oom.Score(proc) > OOM_THRESHOLD) {\n    var victims = procs\n        .Where(p => p.OomAdj >= 0)\n        .OrderByDescending(p => p.OomScore)\n        .Take(MAX_VICTIMS)\n        .ToList();\n    foreach (var v in victims) {\n        log.Warn($"oom kill pid:{v.Pid} ({v.Name}) rss={v.Rss}K");\n        v.Kill(Signal.SIGKILL);\n        freed += v.Rss;\n        if (freed >= needed) break;\n    }\n    if (freed < needed) {\n        log.Fatal("oom: insufficient memory after kills");\n        kernel.Panic("Out of memory");\n    }\n}',
        'for (int b = 0; b < wal.BlockCount; b++) {\n    var blk = wal.ReadBlock(b);\n    db.ApplyBlock(blk);\n    if (b % WAL_CHECKPOINT_EVERY == 0) {\n        wal.Checkpoint();\n        db.Sync();\n        log.Debug($"wal checkpoint @{b}/{wal.BlockCount}");\n    }\n    if (!db.IsConsistent()) {\n        log.Error($"inconsistency after block {b}");\n        db.RollbackToSnapshot(lastSnapshot);\n        wal.Truncate(b);\n        break;\n    }\n}',
        // 100 more
        'Task.Run(async () => {\n    await foreach (var evt in stream.ReadAllAsync(cts.Token)) {\n        switch (evt.Type) {\n            case EventType.GripUpdate:\n                bim.Feed(evt.Payload);\n                break;\n            case EventType.Disconnect:\n                bci.Reconnect();\n                break;\n            case EventType.Calibrate:\n                await bim.RecalibrateAsync();\n                break;\n            default:\n                log.Warn($"unhandled event {evt.Type}");\n                break;\n        }\n    }\n});',
        'for (int attempt = 0; attempt < MAX_RETRIES; attempt++) {\n    try {\n        var result = await gate.AuthorizeAsync(\n            token, scope, cts.Token);\n        if (result.IsGranted) {\n            session.Open(result.Claims);\n            audit.Log(AuditEvent.Login, token.Subject);\n            return result;\n        }\n        log.Warn($"auth denied: {result.Reason}");\n        await Task.Delay(DENY_COOLDOWN_MS);\n    } catch (TokenExpiredException) {\n        token = await token.RefreshAsync();\n    } catch (GateOfflineException) {\n        if (attempt == MAX_RETRIES - 1) throw;\n        await Task.Delay(OFFLINE_BACKOFF_MS << attempt);\n    }\n}\nthrow new AuthFailedException(MAX_RETRIES);',
        'while (net.HasPendingFrames) {\n    var frame = net.Dequeue();\n    if (!tls.Verify(frame)) {\n        dropped++;\n        if (dropped > MAX_DROPS) {\n            log.Error("too many invalid frames — closing");\n            net.Close();\n            break;\n        }\n        continue;\n    }\n    switch (frame.Type) {\n        case FrameType.Data:\n            rx.Append(frame.Payload);\n            break;\n        case FrameType.Ack:\n            tx.Acknowledge(frame.Seq);\n            break;\n        case FrameType.Reset:\n            net.Reset();\n            rx.Clear(); tx.Clear();\n            break;\n    }\n}',
        'foreach (var cert in chain) {\n    if (cert.NotAfter < DateTime.UtcNow) {\n        throw new CertExpiredException(\n            $"{cert.Subject} expired {cert.NotAfter}");\n    }\n    if (revoked.Contains(cert.Thumbprint)) {\n        throw new CertRevokedException(cert.Subject);\n    }\n    if (!prev.PublicKey.VerifySignature(\n            cert.TbsBytes, cert.Signature)) {\n        throw new ChainBrokenException(\n            $"sig invalid at depth {depth}");\n    }\n    depth++;\n    prev = cert;\n}',
        'for (int gen = GcGeneration.Young;\n     gen <= GcGeneration.Old; gen++) {\n    var roots = gc.ScanRoots(gen);\n    foreach (var root in roots) {\n        gc.Mark(root);\n    }\n    var swept = gc.Sweep(gen);\n    log.Debug(\n        $"gc gen{gen}: marked={gc.Marked} swept={swept}B");\n    if (gen == GcGeneration.Old &&\n        heap.Fragmentation > COMPACT_THRESHOLD) {\n        heap.Compact();\n        log.Info("heap compacted after full gc");\n    }\n}',
        'while (!shutdown.IsRequested) {\n    var batch = await ingest.ReadBatchAsync(\n        BATCH_SIZE, cts.Token);\n    if (batch.Count == 0) {\n        await Task.Delay(IDLE_DELAY_MS);\n        continue;\n    }\n    for (int i = 0; i < batch.Count; i++) {\n        try {\n            await processor.HandleAsync(batch[i]);\n            metrics.Processed++;\n        } catch (RetryableException ex) {\n            log.Warn($"retry item {i}: {ex.Message}");\n            await ingest.RequeueAsync(batch[i]);\n        } catch (PoisonException) {\n            log.Error($"poison item {i} — discarding");\n            metrics.Poisoned++;\n        }\n    }\n}',
        'if (bci.IsLinked && !manual.IsActive) {\n    var raw = new float[CONTACT_RINGS];\n    for (int r = 0; r < CONTACT_RINGS; r++) {\n        raw[r] = adc.Sample(r);\n    }\n    var intent = bim.Classify(raw);\n    if (intent != prev && confidence >= MIN_CONF) {\n        switch (intent) {\n            case GripIntent.Precision:\n                await rbs.SelectAsync(Port.Beta);\n                led.Set(Blue); pin.Extend();\n                break;\n            case GripIntent.Sweep:\n                await rbs.SelectAsync(Port.Alpha);\n                led.Set(Red); pin.Retract();\n                break;\n        }\n        prev = intent;\n    }\n}',
        'Task.Run(async () => {\n    using var timer = new PeriodicTimer(\n        TimeSpan.FromMilliseconds(SCHED_INTERVAL));\n    while (await timer.WaitForNextTickAsync(cts.Token)) {\n        for (int i = 0; i < workers.Count; i++) {\n            if (!workers[i].IsIdle) continue;\n            var job = scheduler.Dequeue(\n                workers[i].Affinity);\n            if (job == null) continue;\n            await workers[i].RunAsync(job);\n            metrics.Dispatched++;\n        }\n        if (scheduler.Depth > OVERFLOW_THRESHOLD) {\n            log.Warn($"queue depth={scheduler.Depth}");\n            await workers.ScaleAsync(+SCALE_STEP);\n        }\n    }\n});',
        'for (int row = 0; row < db.PageCount; row++) {\n    var page = db.ReadPage(row);\n    if (page.Magic != DB_MAGIC) {\n        log.Error($"bad magic page {row}: {page.Magic:X}");\n        corrupt.Add(row);\n        continue;\n    }\n    for (int slot = 0; slot < page.SlotCount; slot++) {\n        var rec = page.ReadSlot(slot);\n        if (!rec.IsAlive) continue;\n        if (idx.Contains(rec.Key)) {\n            log.Warn($"duplicate key {rec.Key} at p{row}s{slot}");\n            duplicates++;\n        } else {\n            idx.Add(rec.Key, new Pointer(row, slot));\n        }\n    }\n}',
        'while (jit.TraceBuf.Count > 0) {\n    var trace = jit.TraceBuf.Dequeue();\n    if (trace.HotCount < TRACE_HOT) continue;\n    try {\n        var ir   = tracer.BuildIR(trace);\n        var ssa  = ssa.Convert(ir);\n        var opt1 = dce.Run(ssa);\n        var opt2 = gvn.Run(opt1);\n        var opt3 = licm.Run(opt2);\n        var mc   = regAlloc.Allocate(opt3);\n        var ptr  = emitter.Emit(mc);\n        jit.PatchTrace(trace.EntryAddr, ptr);\n    } catch (TraceAbortException ex) {\n        log.Debug($"trace abort: {ex.Reason}");\n        jit.Blacklist(trace.EntryAddr);\n    }\n}',
        'foreach (var edge in graph.Edges\n    .Where(e => !e.IsValid(DateTime.UtcNow))\n    .ToList()) {\n    graph.RemoveEdge(edge);\n    var src  = graph.GetNode(edge.SourceId);\n    var dst  = graph.GetNode(edge.TargetId);\n    if (src != null) src.Degree--;\n    if (dst != null) dst.Degree--;\n    if (src?.Degree == 0 && src.IsPrunable) {\n        graph.RemoveNode(src.Id);\n        pruned.Nodes++;\n    }\n    pruned.Edges++;\n}\nlog.Info($"graph pruned: {pruned.Nodes}n {pruned.Edges}e");',
        'do {\n    token = await oauth.RequestAsync(\n        clientId, scope, cts.Token);\n    if (token.IsValid) break;\n    log.Warn($"token invalid: {token.Error}");\n    if (token.Error == OAuthError.Revoked) {\n        await oauth.RevokeAsync(token);\n        throw new AuthRevokedException();\n    }\n    retries++;\n    if (retries >= MAX_TOKEN_RETRIES) {\n        throw new TokenExhaustedException();\n    }\n    await Task.Delay(\n        TOKEN_BACKOFF_MS * (1 << retries));\n} while (!token.IsValid);',
        'for (int pass = 0; pass < ENCRYPT_PASSES; pass++) {\n    for (int blk = 0; blk < data.BlockCount; blk++) {\n        var plain  = data.ReadBlock(blk);\n        var iv     = rng.NextBytes(IV_LEN);\n        var cipher = aes.Encrypt(plain, iv, key);\n        var tag    = hmac.Sign(cipher, authKey);\n        out.WriteBlock(blk, iv, cipher, tag);\n        if (blk % PROGRESS_INTERVAL == 0) {\n            progress?.Report(\n                (pass * data.BlockCount + blk) /\n                (float)(ENCRYPT_PASSES * data.BlockCount));\n        }\n    }\n    key = kdf.Derive(key, salt, 1);\n}',
        'while (socket.State == WebSocketState.Open) {\n    var result = await socket.ReceiveAsync(\n        buffer, cts.Token);\n    if (result.MessageType == Close) {\n        await socket.CloseAsync(\n            WebSocketCloseStatus.NormalClosure,\n            "bye", cts.Token);\n        break;\n    }\n    accumulated.Write(\n        buffer.Array, 0, result.Count);\n    if (result.EndOfMessage) {\n        var msg = Encoding.UTF8.GetString(\n            accumulated.ToArray());\n        await dispatcher.HandleAsync(msg);\n        accumulated.SetLength(0);\n    }\n}',
        'Task.Run(async () => {\n    var sema = new SemaphoreSlim(\n        MAX_CONCURRENT_OPS);\n    var tasks = items.Select(async item => {\n        await sema.WaitAsync(cts.Token);\n        try {\n            await processor.ProcessAsync(\n                item, cts.Token);\n            Interlocked.Increment(ref done);\n        } catch (Exception ex) {\n            log.Error(\n                $"process {item.Id}: {ex.Message}");\n            Interlocked.Increment(ref failed);\n        } finally {\n            sema.Release();\n        }\n    });\n    await Task.WhenAll(tasks);\n    log.Info($"done={done} failed={failed}");\n});',
        'for (int i = 0; i < bitmap.Width; i++) {\n    for (int j = 0; j < bitmap.Height; j++) {\n        int idx = (j * bitmap.Stride + i * 4);\n        float lum = (\n            pixels[idx + 0] * 0.2126f +\n            pixels[idx + 1] * 0.7152f +\n            pixels[idx + 2] * 0.0722f) / 255f;\n        float alpha = tornEdge.Sample(i, j);\n        pixels[idx + 3] = (byte)(\n            pixels[idx + 3] * alpha *\n            (0.6f + lum * 0.4f));\n    }\n}',
        'switch (packet.Protocol) {\n    case Protocol.Tcp:\n        if (!tcp.IsValidChecksum(packet)) {\n            dropped++; break;\n        }\n        switch (tcp.Flags) {\n            case TcpFlags.Syn:\n                connTable.OpenHalf(packet);\n                break;\n            case TcpFlags.SynAck:\n                connTable.Complete(packet);\n                break;\n            case TcpFlags.Fin:\n            case TcpFlags.Rst:\n                connTable.Close(packet);\n                break;\n        }\n        break;\n    case Protocol.Udp:\n        udp.Route(packet);\n        break;\n    case Protocol.Icmp:\n        if (firewall.AllowIcmp) icmp.Handle(packet);\n        break;\n}',
        'while (compressor.HasInput) {\n    var block = compressor.ReadInput(BLOCK_SIZE);\n    var lz4   = Lz4.Compress(block);\n    if (lz4.Length >= block.Length) {\n        out.WriteUncompressed(block);\n        stats.Uncompressed += block.Length;\n    } else {\n        out.WriteCompressed(lz4);\n        stats.Compressed   += block.Length;\n        stats.Saved        += block.Length - lz4.Length;\n    }\n    if (out.Position % FLUSH_INTERVAL == 0) {\n        await out.FlushAsync();\n    }\n}\nlog.Info($"ratio={stats.Ratio:F2} saved={stats.Saved}B");',
        'foreach (var proc in procTable.Values\n    .Where(p => p.State == ProcState.Zombie)\n    .ToList()) {\n    if (!proc.Parent.IsAlive) {\n        proc.Reparent(init);\n    }\n    var exit = proc.Reap();\n    log.Debug(\n        $"reaped pid:{proc.Pid} exit:{exit.Code}");\n    if (exit.Signal != Signal.None) {\n        log.Warn(\n            $"pid:{proc.Pid} killed by {exit.Signal}");\n    }\n    procTable.Remove(proc.Pid);\n    resources.Release(proc);\n}',
        'for (int lvl = 0; lvl < BTREE_LEVELS; lvl++) {\n    var node = btree.ReadNode(cursor.NodeId);\n    if (node.IsLeaf) {\n        var slot = node.FindSlot(key);\n        if (slot < 0) return null;\n        return node.ReadValue(slot);\n    }\n    int child = -1;\n    for (int k = 0; k < node.KeyCount; k++) {\n        if (key.CompareTo(node.Keys[k]) < 0) {\n            child = node.Children[k];\n            break;\n        }\n    }\n    if (child < 0)\n        child = node.Children[node.KeyCount];\n    cursor.NodeId = child;\n}',
        'Task.Run(async () => {\n    while (!cts.IsCancellationRequested) {\n        var snap = metrics.Snapshot();\n        foreach (var (key, val) in snap) {\n            await telemetry.EmitAsync(\n                key, val, DateTime.UtcNow);\n        }\n        if (snap.TryGetValue("heap_rss", out var rss)\n            && rss > RSS_ALERT_THRESHOLD) {\n            await alerting.FireAsync(\n                Alert.HighMemory, rss);\n        }\n        if (snap.TryGetValue("error_rate", out var er)\n            && er > ERROR_RATE_THRESHOLD) {\n            await alerting.FireAsync(\n                Alert.HighErrorRate, er);\n        }\n        await Task.Delay(METRICS_INTERVAL_MS);\n    }\n});',
        'for (int i = 0; i < nodes.Count; i++) {\n    for (int j = i + 1; j < nodes.Count; j++) {\n        if (!graph.HasEdge(nodes[i], nodes[j])) {\n            continue;\n        }\n        float w = similarity.Compute(\n            nodes[i].Embedding,\n            nodes[j].Embedding);\n        if (w < PRUNE_THRESHOLD) {\n            graph.RemoveEdge(nodes[i], nodes[j]);\n            pruned++;\n        } else if (w > MERGE_THRESHOLD) {\n            candidates.Enqueue(\n                (nodes[i], nodes[j], w));\n        }\n    }\n}',
        'while (input.HasMore) {\n    var token = lexer.Next();\n    switch (token.Kind) {\n        case TokenKind.If:\n            ast.Push(new IfNode());\n            break;\n        case TokenKind.While:\n            ast.Push(new WhileNode());\n            break;\n        case TokenKind.For:\n            ast.Push(new ForNode());\n            break;\n        case TokenKind.LBrace:\n            ast.OpenBlock();\n            break;\n        case TokenKind.RBrace:\n            var block = ast.CloseBlock();\n            ast.Peek().AddChild(block);\n            break;\n        case TokenKind.Eof:\n            return ast.Root;\n    }\n}',
        'foreach (var region in memMap.Regions) {\n    if (!region.Flags.HasFlag(MemFlags.Write)) {\n        continue;\n    }\n    for (ulong addr = region.Base;\n         addr < region.Base + region.Size;\n         addr += PAGE_SIZE) {\n        var page = mem.ReadPage(addr);\n        if (page.IsDirty) {\n            var hash = sha256.Hash(page.Data);\n            if (hash != page.ExpectedHash) {\n                tampered.Add(addr);\n                log.Warn(\n                    $"page tamper @{addr:X16}");\n            }\n            page.ClearDirty();\n        }\n    }\n}',
        'for (int r = 0; r < MAX_RETRIES; r++) {\n    try {\n        using var tx = db.BeginTransaction(\n            IsolationLevel.Serializable);\n        var existing = tx.Find<Node>(id);\n        if (existing != null) {\n            existing.Update(payload);\n        } else {\n            tx.Insert(new Node(id, payload));\n        }\n        await tx.CommitAsync();\n        return;\n    } catch (SerializationConflict) {\n        if (r == MAX_RETRIES - 1) throw;\n        await Task.Delay(\n            TX_BACKOFF_MS * (1 << r));\n    }\n}',
        'Task.Run(async () => {\n    var ring = new RingBuffer<Event>(RING_SIZE);\n    while (!cts.IsCancellationRequested) {\n        int count = producer.Drain(ring);\n        for (int i = 0; i < count; i++) {\n            var ev = ring[i];\n            switch (ev.Category) {\n                case Category.Audit:\n                    await auditLog.WriteAsync(ev);\n                    break;\n                case Category.Metric:\n                    metrics.Record(ev);\n                    break;\n                case Category.Alert:\n                    await alerting.NotifyAsync(ev);\n                    break;\n            }\n        }\n        if (count == 0)\n            await Task.Yield();\n    }\n});',
        'while (fsm.State != State.Terminal) {\n    var input = fsm.NextInput();\n    switch (fsm.State) {\n        case State.Idle:\n            if (input.Is(Input.Connect))\n                fsm.Transition(State.Handshake);\n            break;\n        case State.Handshake:\n            if (!tls.Complete(input))\n                fsm.Transition(State.Error);\n            else\n                fsm.Transition(State.Active);\n            break;\n        case State.Active:\n            session.Process(input);\n            if (input.Is(Input.Close))\n                fsm.Transition(State.Closing);\n            break;\n        case State.Error:\n            log.Error($"fsm error: {input}");\n            fsm.Transition(State.Terminal);\n            break;\n    }\n}',
        'for (int oct = 0; oct < OCTAVES; oct++) {\n    float freq  = BASE_FREQ * MathF.Pow(2f, oct);\n    float amp   = BASE_AMP  * MathF.Pow(PERSIST, oct);\n    for (int y = 0; y < H; y++) {\n        for (int x = 0; x < W; x++) {\n            float nx = x * freq / W;\n            float ny = y * freq / H;\n            noise[y * W + x] +=\n                vnoise(nx, ny, seed + oct) * amp;\n        }\n    }\n}\nfor (int i = 0; i < W * H; i++) {\n    noise[i] = (noise[i] + 1f) * 0.5f;\n    noise[i] = MathF.Pow(noise[i], CONTRAST);\n}',
        'foreach (var tx in pending.OrderBy(t => t.Fee)) {\n    if (block.ByteSize + tx.Size > MAX_BLOCK_SIZE)\n        break;\n    if (utxo.IsDoubleSpend(tx)) {\n        log.Warn($"double spend tx {tx.Hash}");\n        continue;\n    }\n    foreach (var input in tx.Inputs) {\n        if (!utxo.Contains(input.OutPoint)) {\n            log.Warn($"missing utxo {input.OutPoint}");\n            goto nextTx;\n        }\n    }\n    utxo.Apply(tx);\n    block.AddTransaction(tx);\n    nextTx:;\n}',
        'Task.Run(async () => {\n    var limiter = new RateLimiter(\n        RATE_LIMIT, TimeSpan.FromSeconds(1));\n    await foreach (var req in requests\n        .ReadAllAsync(cts.Token)) {\n        if (!await limiter.TryAcquireAsync()) {\n            await req.RespondAsync(\n                StatusCode.TooManyRequests);\n            metrics.Throttled++;\n            continue;\n        }\n        var resp = await handler.HandleAsync(req);\n        await req.RespondAsync(resp);\n        metrics.Handled++;\n    }\n});',
        'for (int i = threadpool.MinThreads;\n     i <= threadpool.MaxThreads; i++) {\n    var w = new Worker(i, queue);\n    workers.Add(w);\n    w.Start();\n    if (queue.Depth < SCALE_DOWN_THRESHOLD\n        && i > threadpool.MinThreads) {\n        log.Debug($"pool: capped at {i} workers");\n        break;\n    }\n}\nlog.Info(\n    $"threadpool: {workers.Count} workers active");',
        'while (cts.IsCancellationRequested == false) {\n    var req = await listener.AcceptAsync(cts.Token);\n    _ = Task.Run(async () => {\n        try {\n            if (!rateLimit.Allow(req.RemoteEp)) {\n                await req.RejectAsync(429);\n                return;\n            }\n            var resp = await router.RouteAsync(req);\n            await req.SendAsync(resp);\n            access.Log(req, resp);\n        } catch (Exception ex) {\n            log.Error(\n                $"req {req.Id}: {ex.Message}");\n            await req.RejectAsync(500);\n        }\n    }, cts.Token);\n}',
        'for (int page = startPage;\n     page <= endPage; page++) {\n    if (snapshot.Bitmap.IsSet(page)) {\n        var data = cow.ReadVersion(page, version);\n        out.WritePage(page, data);\n    } else {\n        var data = db.ReadPage(page);\n        out.WritePage(page, data);\n        snapshot.Bitmap.Set(page);\n    }\n    if ((page - startPage) % FLUSH_PAGES == 0) {\n        await out.FlushAsync();\n        progress?.Report(\n            (page - startPage) /\n            (float)(endPage - startPage));\n    }\n}',
        'switch (sig.Value) {\n    case Signal.SIGINT:\n    case Signal.SIGTERM:\n        log.Info($"caught {sig} — graceful shutdown");\n        await server.StopAsync(GRACEFUL_TIMEOUT);\n        break;\n    case Signal.SIGHUP:\n        log.Info("SIGHUP — reloading config");\n        await config.ReloadAsync();\n        await server.ReconfigureAsync(config);\n        break;\n    case Signal.SIGUSR1:\n        log.Info("SIGUSR1 — rotating logs");\n        await logger.RotateAsync();\n        break;\n    case Signal.SIGSEGV:\n        log.Fatal("SIGSEGV — crash dump");\n        await core.DumpAsync();\n        Environment.FailFast("SIGSEGV");\n        break;\n}',
        'Task.Run(async () => {\n    var pending = new List<LogEntry>();\n    while (!cts.IsCancellationRequested) {\n        var entry = await logQueue\n            .ReadAsync(cts.Token);\n        pending.Add(entry);\n        if (pending.Count >= FLUSH_BATCH ||\n            (DateTime.UtcNow - pending[0].Time)\n                > FLUSH_INTERVAL) {\n            try {\n                await sink.WriteBatchAsync(pending);\n                pending.Clear();\n            } catch (Exception ex) {\n                log.Error(\n                    $"log flush failed: {ex.Message}");\n                await Task.Delay(RETRY_DELAY);\n            }\n        }\n    }\n    if (pending.Count > 0)\n        await sink.WriteBatchAsync(pending);\n});',
        'for (int seg = 0; seg < segs.Count; seg++) {\n    var s = segs[seg];\n    if (s.Compression == Codec.None) {\n        out.Write(s.Data);\n    } else if (s.Compression == Codec.Lz4) {\n        out.Write(Lz4.Decompress(\n            s.Data, s.UncompressedLen));\n    } else if (s.Compression == Codec.Zstd) {\n        out.Write(Zstd.Decompress(s.Data));\n    } else {\n        throw new UnknownCodecException(\n            s.Compression);\n    }\n    if (s.Checksum != Crc32.Compute(s.Data)) {\n        throw new SegmentCorruptException(seg);\n    }\n}',
        'while (!input.IsEof) {\n    int ch = input.Peek();\n    if (ch == \'/\') {\n        input.Advance();\n        if (input.Peek() == \'/\') {\n            while (!input.IsEol) input.Advance();\n        } else if (input.Peek() == \'*\') {\n            while (!input.IsEof) {\n                input.Advance();\n                if (prev == \'*\' && input.Peek() == \'/\') {\n                    input.Advance(); break;\n                }\n                prev = (char)input.Peek();\n            }\n        } else {\n            yield return Token(TokenKind.Slash);\n        }\n    } else {\n        yield return Scan(input);\n    }\n}',
        'foreach (var shard in ring\n    .GetPreferredNodes(key, REPLICAS)) {\n    var ok = false;\n    for (int t = 0; t < WRITE_TRIES; t++) {\n        try {\n            await shard.PutAsync(\n                key, value, ttl, cts.Token);\n            ok = true; break;\n        } catch (NodeUnavailableException) {\n            var next = ring.NextNode(shard);\n            if (next == null) break;\n            shard = next;\n        }\n    }\n    if (ok) acks++;\n    if (acks >= WRITE_QUORUM) return;\n}\nif (acks < WRITE_QUORUM)\n    throw new QuorumException(acks, WRITE_QUORUM);',
        'Task.Run(async () => {\n    await using var conn = await pool.RentAsync();\n    while (!cts.IsCancellationRequested) {\n        var changes = await cdc.PollAsync(\n            lastLsn, cts.Token);\n        foreach (var ch in changes) {\n            switch (ch.Op) {\n                case CdcOp.Insert:\n                    await sink.OnInsert(ch.Row);\n                    break;\n                case CdcOp.Update:\n                    await sink.OnUpdate(\n                        ch.OldRow, ch.Row);\n                    break;\n                case CdcOp.Delete:\n                    await sink.OnDelete(ch.Row);\n                    break;\n            }\n            lastLsn = ch.Lsn;\n        }\n    }\n});',
        'for (int level = 0;\n     level < lsm.LevelCount; level++) {\n    var files = lsm.GetLevel(level);\n    if (files.Count < lsm.LevelMaxFiles(level))\n        continue;\n    var merged = Sstable.Merge(files);\n    foreach (var f in files) {\n        lsm.Remove(f);\n        f.Delete();\n    }\n    if (level + 1 < lsm.LevelCount) {\n        lsm.AddToLevel(level + 1, merged);\n    } else {\n        lsm.AddNewLevel(merged);\n    }\n    log.Info(\n        $"compacted L{level}: {files.Count}→1");\n}',
        'while (reader.TryRead(out var span)) {\n    for (int i = 0; i < span.Length; i++) {\n        ref var sample = ref span[i];\n        if (sample.Value > CLIP_HIGH) {\n            sample.Value = CLIP_HIGH;\n            clipped++;\n        } else if (sample.Value < CLIP_LOW) {\n            sample.Value = CLIP_LOW;\n            clipped++;\n        }\n        sum += sample.Value;\n        sumSq += sample.Value * sample.Value;\n        count++;\n    }\n    if (count % STATS_INTERVAL == 0) {\n        float mean = sum / count;\n        float var_ = sumSq / count - mean * mean;\n        telemetry.Record("signal_var", var_);\n    }\n}',
        'foreach (var group in events\n    .GroupBy(e => e.Category)\n    .OrderByDescending(g => g.Count())) {\n    var agg = new Aggregate {\n        Category = group.Key,\n        Count    = group.Count(),\n        Total    = group.Sum(e => e.Value),\n        P50      = group.Percentile(e => e.Value, 50),\n        P99      = group.Percentile(e => e.Value, 99),\n    };\n    if (agg.P99 > SLA_THRESHOLD) {\n        await alerting.FireAsync(\n            Alert.SlaBreach, agg);\n    }\n    await metrics.EmitAsync(agg);\n}',
        'Task.Run(async () => {\n    var buf = new byte[RECV_BUF];\n    while (true) {\n        int n = await udp.ReceiveAsync(\n            buf, cts.Token);\n        if (n < HEADER_LEN) continue;\n        var hdr = Header.Parse(buf, n);\n        if (!hdr.IsValid) {\n            malformed++; continue;\n        }\n        if (seen.Contains(hdr.Nonce)) {\n            replays++; continue;\n        }\n        seen.Add(hdr.Nonce);\n        var plain = chacha.Decrypt(\n            buf, HEADER_LEN, n - HEADER_LEN,\n            hdr.Nonce, sessionKey);\n        if (plain == null) {\n            authFails++; continue;\n        }\n        await dispatcher.HandleAsync(plain);\n    }\n});',
        'for (int r = 0; r < rows; r++) {\n    for (int c = 0; c < cols; c++) {\n        float s = 0f;\n        for (int k = 0; k < K; k++) {\n            s += A[r * K + k] * B[k * cols + c];\n        }\n        C[r * cols + c] = s;\n    }\n    if (r % PROGRESS_ROWS == 0) {\n        float pct = r / (float)rows * 100f;\n        log.Debug(\n            $"matmul: {pct:F0}% ({r}/{rows})");\n    }\n}',
        'while (!drain.IsComplete) {\n    var slot = ring.TryAcquireSlot();\n    if (slot < 0) {\n        await Task.Yield();\n        continue;\n    }\n    try {\n        var item = source.Take(cts.Token);\n        ring.Write(slot, Serialize(item));\n        ring.Publish(slot);\n        produced++;\n    } catch (InvalidOperationException) {\n        ring.Release(slot);\n        drain.Signal();\n    } catch (OperationCanceledException) {\n        ring.Release(slot);\n        break;\n    }\n}',
        'foreach (var (node, depth) in bfs.Traverse(root)) {\n    if (depth > MAX_DEPTH) {\n        bfs.Prune(node);\n        continue;\n    }\n    switch (node.Type) {\n        case NodeType.Entity:\n            index.Add(node.Id, node);\n            break;\n        case NodeType.Relation:\n            graph.Link(\n                node.SourceId,\n                node.TargetId,\n                node.Weight);\n            break;\n        case NodeType.Aggregate:\n            stats.Accumulate(node);\n            break;\n    }\n    if (node.HasChildren)\n        bfs.Enqueue(node.Children, depth + 1);\n}',
        'Task.Run(async () => {\n    await using var cursor = db.OpenCursor(\n        table, scanOrder);\n    while (await cursor.MoveNextAsync()) {\n        var row = cursor.Current;\n        if (predicate(row)) {\n            buffer.Add(row);\n            if (buffer.Count >= BATCH_SIZE) {\n                await handler.HandleBatchAsync(\n                    buffer);\n                buffer.Clear();\n            }\n        }\n    }\n    if (buffer.Count > 0) {\n        await handler.HandleBatchAsync(buffer);\n    }\n    log.Info(\n        $"scan done: {cursor.Scanned} rows");\n});',
        'for (int i = 0; i < proof.Length; i++) {\n    if (i % 2 == 0) {\n        current = sha256.Hash(\n            Concat(current, proof[i]));\n    } else {\n        current = sha256.Hash(\n            Concat(proof[i], current));\n    }\n    if (current == null ||\n        current.Length != HASH_LEN) {\n        throw new MerkleInvalidException(\n            $"bad hash at depth {i}");\n    }\n}\nif (!current.SequenceEqual(root)) {\n    throw new MerkleProofException();\n}',
        'while (freeList.Count < MIN_FREE_PAGES) {\n    var victim = clock.NextVictim();\n    if (victim == null) break;\n    if (victim.IsPinned) {\n        clock.Skip(victim);\n        continue;\n    }\n    if (victim.IsDirty) {\n        await disk.WriteAsync(\n            victim.PageId, victim.Data);\n        dirty--;\n    }\n    bufferPool.Evict(victim);\n    freeList.Add(victim.Frame);\n    evictions++;\n}',
        'foreach (var pipeline in pipelines) {\n    var result = pipeline.Source;\n    foreach (var stage in pipeline.Stages) {\n        try {\n            result = await stage.ProcessAsync(\n                result, cts.Token);\n        } catch (StageException ex) {\n            if (stage.CanBypass) {\n                log.Warn(\n                    $"bypass {stage.Name}: {ex.Message}");\n                continue;\n            }\n            throw;\n        }\n    }\n    await pipeline.Sink.WriteAsync(result);\n}',
        'for (int gen = 0; gen < GENERATIONS; gen++) {\n    var selected = population\n        .OrderByDescending(c => c.Fitness)\n        .Take(ELITE_COUNT)\n        .ToList();\n    while (selected.Count < POP_SIZE) {\n        var p1 = tournament.Select(population);\n        var p2 = tournament.Select(population);\n        var child = crossover.Apply(p1, p2);\n        if (rng.NextDouble() < MUTATION_RATE) {\n            mutation.Apply(child);\n        }\n        selected.Add(child);\n    }\n    population = selected;\n    var best = population.Max(c => c.Fitness);\n    log.Debug($"gen {gen}: best={best:F4}");\n}',
        'Task.Run(async () => {\n    var debounce = new Debouncer(\n        TimeSpan.FromMilliseconds(DEBOUNCE_MS));\n    await foreach (var change in watcher\n        .WatchAsync(cts.Token)) {\n        debounce.Trigger(change.Path, async () => {\n            try {\n                await handler.OnChangeAsync(\n                    change);\n                log.Info(\n                    $"reloaded {change.Path}");\n            } catch (Exception ex) {\n                log.Error(\n                    $"reload fail: {ex.Message}");\n            }\n        });\n    }\n});',
        'while (backpressure.IsHighWatermark) {\n    var evicted = 0;\n    foreach (var entry in cache\n        .OrderBy(e => e.LastAccess)\n        .Take(EVICT_BATCH)) {\n        if (entry.IsPinned) continue;\n        cache.Remove(entry.Key);\n        evicted++;\n        if (!backpressure.IsHighWatermark)\n            break;\n    }\n    if (evicted == 0) {\n        log.Warn("cache pressure: nothing evictable");\n        await Task.Delay(BACKPRESSURE_WAIT_MS);\n    }\n}',
        'for (int seg = segStart; seg < segEnd; seg++) {\n    var hdr = elf.ReadSegmentHeader(seg);\n    if (hdr.Type != PT_LOAD) continue;\n    var data = elf.ReadSegment(seg);\n    var vaddr = hdr.VAddr + baseAddr;\n    mem.Map(vaddr, hdr.MemSize, hdr.Flags);\n    mem.Write(vaddr, data);\n    if (data.Length < hdr.MemSize) {\n        mem.Zero(\n            vaddr + data.Length,\n            hdr.MemSize - data.Length);\n    }\n    if (hdr.Flags.HasFlag(PF_X)) {\n        cache.FlushICache(vaddr, hdr.MemSize);\n    }\n}',
        'foreach (var bucket in histogram\n    .GetBuckets()\n    .OrderBy(b => b.LowerBound)) {\n    float density =\n        bucket.Count / (float)histogram.Total;\n    for (int col = 0;\n         col < (int)(density * MAX_COLS);\n         col++) {\n        row.Append(\'█\');\n    }\n    row.Append(\n        $" [{bucket.LowerBound:F2}–"\n        + $"{bucket.UpperBound:F2}]"\n        + $" n={bucket.Count}");\n    output.WriteLine(row.ToString());\n    row.Clear();\n}',
        'Task.Run(async () => {\n    using var scope = services.CreateScope();\n    var repo = scope.Get<INodeRepository>();\n    while (!cts.IsCancellationRequested) {\n        var stale = await repo.FindStaleAsync(\n            olderThan: DateTime.UtcNow - TTL,\n            limit: PURGE_BATCH);\n        if (stale.Count == 0) {\n            await Task.Delay(IDLE_PERIOD_MS);\n            continue;\n        }\n        foreach (var n in stale) {\n            await repo.ArchiveAsync(n);\n            purged++;\n        }\n        log.Info(\n            $"purged {stale.Count} nodes total={purged}");\n    }\n});',
        'for (int i = 0; i < seq.Count - 1; i++) {\n    int gap = seq[i + 1].Timestamp\n            - seq[i].Timestamp;\n    if (gap < MIN_GAP || gap > MAX_GAP) {\n        anomalies.Add(new Anomaly {\n            Index = i,\n            Expected = NOMINAL_GAP,\n            Actual   = gap,\n            Severity = gap > MAX_GAP\n                ? Severity.High\n                : Severity.Low\n        });\n        if (anomalies.Count > MAX_ANOMALIES) {\n            throw new AnomalyFloodException();\n        }\n    }\n}',
        // 20 more
        'for (int i = 0; i < relays.Count; i++) {\n    if (!relays[i].IsReachable) {\n        skipped++; continue;\n    }\n    var rtt = await relays[i].PingAsync(cts.Token);\n    if (rtt > SLA_RTT_MS) {\n        slow.Add(relays[i]);\n        log.Warn(\n            $"relay {relays[i].Id} rtt={rtt}ms > SLA");\n        continue;\n    }\n    if (rtt < bestRtt) {\n        bestRtt   = rtt;\n        preferred = relays[i];\n    }\n}\nif (preferred == null)\n    throw new NoRelayException(skipped);',
        'while (!bci.IsLocked) {\n    var raw = cortex.Sample(SAMPLE_BURST);\n    bim.Feed(raw);\n    lock_attempts++;\n    if (lock_attempts > MAX_LOCK_ATTEMPTS) {\n        log.Fatal(\n            "bci lock attempts exceeded");\n        throw new BciLockoutException();\n    }\n    if (bim.Confidence < MIN_CONFIDENCE) {\n        await Task.Delay(RETRY_DELAY_MS);\n        continue;\n    }\n    if (!bim.ValidateKey(neuralKey)) {\n        sec.Alert(SecEvent.KeyMismatch);\n        await Task.Delay(KEY_RETRY_MS * lock_attempts);\n        continue;\n    }\n    bci.Lock();\n}',
        'foreach (var district in world.Districts\n    .Where(d => d.HasActiveRelay)\n    .OrderBy(d => d.Latency)) {\n    try {\n        var route = await router.FindAsync(\n            source, district.GlmzGate,\n            RouteFlags.DarkNodeOk);\n        if (route.HasBlacklist) {\n            log.Warn(\n                $"route via {district.Id} blacklisted");\n            continue;\n        }\n        await route.OpenAsync(cts.Token);\n        connections[district.Id] = route;\n    } catch (EnforcementBlockException ex) {\n        log.Error(\n            $"blocked at {ex.Gate}: {ex.Reason}");\n        blockedDistricts.Add(district.Id);\n    }\n}',
        'Task.Run(async () => {\n    var window = new SlidingWindow<float>(\n        WINDOW_SIZE);\n    while (!cts.IsCancellationRequested) {\n        var sample = bci.SampleRaw();\n        window.Add(sample);\n        if (window.IsFull) {\n            float mean = window.Mean();\n            float std  = window.StdDev();\n            float z    = Math.Abs(\n                (sample - mean) / std);\n            if (z > ANOMALY_Z) {\n                log.Warn(\n                    $"bci anomaly z={z:F2} at {sample:F4}");\n                anomalies.Record(DateTime.UtcNow, z);\n            }\n        }\n        await Task.Delay(SAMPLE_INTERVAL_MS);\n    }\n});',
        'for (int slot = 0; slot < rbs.SlotCount; slot++) {\n    var pos = rbs.ReadEncoder(slot);\n    if (!rbs.IsValidPosition(pos)) {\n        faults.Add(new EncoderFault {\n            Slot     = slot,\n            Position = pos,\n            Expected = rbs.NominalPosition(slot)\n        });\n        log.Error(\n            $"rbs encoder fault slot={slot} pos={pos}");\n        if (faults.Count >= RBS_FAULT_LIMIT) {\n            weapon.SafetyEngage();\n            throw new RbsEncoderException(faults);\n        }\n        continue;\n    }\n    rbs.Calibrate(slot, pos);\n}',
        'while (vault.IsSealed) {\n    var shards = await keyStore.LoadShardsAsync(\n        vault.Id, cts.Token);\n    int valid = 0;\n    for (int i = 0; i < shards.Count; i++) {\n        if (shards[i].VerifyHmac(masterKey)) {\n            valid++;\n            xorBuf.XorWith(shards[i].Material);\n        } else {\n            log.Warn(\n                $"shard {i} hmac invalid — skip");\n        }\n    }\n    if (valid >= vault.Threshold) {\n        var key = kdf.Derive(\n            xorBuf, vault.Salt, KDF_ITERS);\n        vault.Unseal(key);\n        log.Info(\n            $"vault unsealed {valid}/{shards.Count}");\n    } else {\n        throw new VaultSealException(valid);\n    }\n}',
        'foreach (var op in pendingOps\n    .Where(o => o.IsExpired(DateTime.UtcNow))\n    .ToList()) {\n    log.Warn(\n        $"op {op.Id} expired after {op.Age}ms");\n    switch (op.Type) {\n        case OpType.Write:\n            await journal.AbortAsync(op.TxnId);\n            break;\n        case OpType.Read:\n            op.Fail(new TimeoutException());\n            break;\n        case OpType.Flush:\n            if (!fs.IsFlushing)\n                await fs.FlushAsync();\n            break;\n    }\n    pendingOps.Remove(op);\n    expired++;\n}',
        'Task.Run(async () => {\n    using var watcher = new MemoryWatcher(\n        WATCH_INTERVAL_MS);\n    await foreach (var snap in watcher\n        .StreamAsync(cts.Token)) {\n        var delta = snap.Rss - prevRss;\n        if (delta > LEAK_THRESHOLD_KB * 1024) {\n            leakSamples++;\n            log.Warn(\n                $"rss growth +{delta / 1024}KB sample={leakSamples}");\n            if (leakSamples >= LEAK_CONFIRM_SAMPLES) {\n                await alerting.FireAsync(\n                    Alert.MemoryLeak, snap.Rss);\n                leakSamples = 0;\n            }\n        } else {\n            leakSamples = 0;\n        }\n        prevRss = snap.Rss;\n    }\n});',
        'for (int r = startRow; r < endRow; r++) {\n    var page = db.FetchPage(r / PAGE_ROWS);\n    var rec  = page.GetRow(r % PAGE_ROWS);\n    if (rec.IsDeleted) continue;\n    if (!predicate(rec)) continue;\n    if (rec.Version > snapshotLsn) {\n        var old = mvcc.FindVersion(\n            rec.RowId, snapshotLsn);\n        if (old == null) continue;\n        rec = old;\n    }\n    projection.Project(rec, out var row);\n    yield return row;\n    if (++count >= FETCH_LIMIT) {\n        log.Debug(\n            $"scan limit {FETCH_LIMIT} hit at row {r}");\n        yield break;\n    }\n}',
        'while (tls.HandshakeState != TlsState.Done) {\n    var msg = await net.RecvAsync(cts.Token);\n    switch (tls.HandshakeState) {\n        case TlsState.ClientHello:\n            tls.ProcessClientHello(msg);\n            await net.SendAsync(\n                tls.BuildServerHello());\n            await net.SendAsync(\n                tls.BuildCertificate());\n            break;\n        case TlsState.KeyExchange:\n            tls.ProcessKeyExchange(msg);\n            sessionKey = tls.DeriveKeys();\n            await net.SendAsync(\n                tls.BuildFinished());\n            break;\n        case TlsState.Finished:\n            tls.VerifyFinished(msg);\n            break;\n        default:\n            throw new TlsProtocolException(\n                tls.HandshakeState);\n    }\n}',
        'for (int epoch = 0; epoch < MAX_EPOCHS; epoch++) {\n    float loss = 0f;\n    for (int b = 0; b < batches.Count; b++) {\n        var (x, y) = batches[b];\n        var pred = model.Forward(x);\n        var l    = loss_fn(pred, y);\n        loss    += l;\n        model.Backward(l);\n        if ((b + 1) % ACCUM_STEPS == 0) {\n            optimizer.Step(lr);\n            optimizer.ZeroGrad();\n        }\n    }\n    loss /= batches.Count;\n    log.Info(\n        $"epoch {epoch}: loss={loss:F4} lr={lr:F6}");\n    if (loss < CONVERGE_THRESHOLD) {\n        log.Info("converged");\n        break;\n    }\n    lr *= LR_DECAY;\n}',
        'Task.Run(async () => {\n    var conn = await glmz.ConnectAsync(\n        GlmzNode.DarkNode, cts.Token);\n    using var enc = new FrameEncryptor(\n        sessionKey, conn);\n    while (!cts.IsCancellationRequested) {\n        var payload = await queue.DequeueAsync(\n            cts.Token);\n        var frame = new DarkFrame {\n            Nonce   = rng.NextBytes(12),\n            Payload = payload,\n            Stamp   = DateTime.UtcNow\n        };\n        await enc.SendAsync(frame);\n        metrics.DarkSent++;\n        if (metrics.DarkSent % LOG_INTERVAL == 0) {\n            log.Debug(\n                $"dark node: {metrics.DarkSent} frames");\n        }\n    }\n});',
        'foreach (var node in graph.GetNeighbors(target)\n    .Where(n => n.District == operatorDistrict)\n    .OrderByDescending(n => n.TrustScore)) {\n    if (enforcement.HasWarrant(node.Id)) {\n        log.Warn(\n            $"skip node {node.Id} — warrant active");\n        continue;\n    }\n    var cred = await auth.IssueCredsAsync(\n        node.Id, scope, TTL_HOURS);\n    if (!cred.IsValid) {\n        log.Error(\n            $"cred issue failed: {cred.Reason}");\n        continue;\n    }\n    grants.Add(node.Id, cred);\n    if (grants.Count >= MAX_GRANTS) break;\n}',
        'while (compaction.HasWork) {\n    var run = compaction.NextRun();\n    using var merger = new SstMerger(\n        run.Inputs, run.Output);\n    int written = 0;\n    while (merger.MoveNext()) {\n        var kv = merger.Current;\n        if (kv.IsTombstone &&\n            kv.Seq < compaction.GcHorizon) {\n            continue;\n        }\n        run.Output.Write(kv);\n        written++;\n        if (written % COMPACT_LOG_INTERVAL == 0) {\n            log.Debug(\n                $"compact: {written} kv written");\n        }\n    }\n    compaction.Complete(run);\n    log.Info(\n        $"compact done: {written} kv in {run.Inputs.Count} files");\n}',
        'Task.Run(async () => {\n    var dial  = new Dialer(relayPool);\n    var retry = new ExponentialBackoff(\n        BASE_MS, MAX_MS, JITTER_FACTOR);\n    int attempts = 0;\n    while (!cts.IsCancellationRequested) {\n        try {\n            var conn = await dial.DialAsync(\n                dist12.GlmzEndpoint, cts.Token);\n            log.Info(\n                $"connected after {attempts} attempts");\n            await conn.ServeAsync(handler, cts.Token);\n            retry.Reset();\n            attempts = 0;\n        } catch (OperationCanceledException) {\n            break;\n        } catch (Exception ex) {\n            attempts++;\n            var delay = retry.Next();\n            log.Warn(\n                $"dial fail #{attempts}: {ex.Message} — retry in {delay}ms");\n            await Task.Delay(delay, cts.Token);\n        }\n    }\n});',
        'for (int i = 0; i < ops.Count; i++) {\n    var op = ops[i];\n    if (!authorization.Check(\n            op.Uid, op.Resource, op.Action)) {\n        audit.Record(AuditEvent.Denied,\n            op.Uid, op.Resource, op.Action);\n        denied.Add(i);\n        continue;\n    }\n    try {\n        var result = await executor.RunAsync(\n            op, cts.Token);\n        results[i] = result;\n        audit.Record(AuditEvent.Allowed,\n            op.Uid, op.Resource, op.Action);\n    } catch (PolicyException ex) {\n        audit.Record(AuditEvent.PolicyViolation,\n            op.Uid, ex.Rule);\n        results[i] = Result.Deny(ex.Rule);\n    }\n}',
        'while (recv.HasData) {\n    var seg = recv.ReadSegment();\n    if (seg.Seq != expected) {\n        outOfOrder.Enqueue(seg);\n        if (outOfOrder.Count > OOO_LIMIT) {\n            log.Error("ooo buffer overflow — reset");\n            recv.SendReset();\n            recv.Clear();\n            outOfOrder.Clear();\n            expected = 0;\n            break;\n        }\n        continue;\n    }\n    reassembly.Write(seg.Data);\n    expected = seg.Seq + seg.Data.Length;\n    while (outOfOrder.TryPeek(out var next)\n        && next.Seq == expected) {\n        outOfOrder.Dequeue();\n        reassembly.Write(next.Data);\n        expected = next.Seq + next.Data.Length;\n    }\n}',
        'foreach (var alert in monitor.GetPending()\n    .OrderByDescending(a => a.Severity)\n    .Take(MAX_ALERTS_PER_TICK)) {\n    switch (alert.Severity) {\n        case Severity.Critical:\n            await pager.PageAsync(\n                oncall.Primary, alert);\n            await pager.PageAsync(\n                oncall.Secondary, alert);\n            break;\n        case Severity.High:\n            await pager.PageAsync(\n                oncall.Primary, alert);\n            break;\n        case Severity.Medium:\n            await email.SendAsync(\n                team.Email, alert);\n            break;\n        default:\n            await slack.PostAsync(\n                CHANNEL_OPS, alert);\n            break;\n    }\n    monitor.Acknowledge(alert.Id);\n}',
        'Task.Run(async () => {\n    var hb = new HeartbeatMonitor(\n        HEARTBEAT_INTERVAL_MS,\n        HEARTBEAT_TIMEOUT_MS);\n    hb.OnTimeout += async (nodeId) => {\n        log.Error(\n            $"node {nodeId} heartbeat timeout");\n        var node = cluster.Get(nodeId);\n        if (node?.IsPrimary == true) {\n            await election.StartAsync();\n        } else {\n            await cluster.MarkDeadAsync(nodeId);\n            await rebalancer.RebalanceAsync();\n        }\n    };\n    await hb.RunAsync(cts.Token);\n});',
        'for (int pass = 0; pass < 3; pass++) {\n    switch (pass) {\n        case 0:\n            foreach (var n in graph.Nodes)\n                n.Color = Color.White;\n            break;\n        case 1:\n            foreach (var n in graph.Nodes\n                .Where(n => n.Color == Color.White)) {\n                dfs.Visit(n);\n            }\n            break;\n        case 2:\n            var scc = new List<Component>();\n            foreach (var n in dfs.FinishOrder) {\n                if (n.Component == null) {\n                    var c = dfs2.Explore(n);\n                    scc.Add(c);\n                }\n            }\n            graph.Components = scc;\n            break;\n    }\n}',
        'while (true) {\n    var acquired = await sem.WaitAsync(\n        ACQUIRE_TIMEOUT_MS);\n    if (!acquired) {\n        log.Warn(\n            $"lock timeout after {ACQUIRE_TIMEOUT_MS}ms");\n        contention++;\n        if (contention > DEADLOCK_THRESHOLD) {\n            log.Fatal("possible deadlock — abort");\n            throw new DeadlockException();\n        }\n        await Task.Delay(\n            BACKOFF_BASE_MS * contention);\n        continue;\n    }\n    try {\n        return await criticalSection();\n    } finally {\n        contention = 0;\n        sem.Release();\n    }\n}',
    ];

    // ── Warning popup data ──────────────────────────────────────────────────
    var WARN_TITLES = [
        'WARN — latency spike',     'WARN — retry limit',       'WARN — signal degraded',
        'WARN — disk 85% full',     'WARN — cert expires 7d',   'WARN — audit gap',
        'WARN — bci drift',         'WARN — temp elevated',     'WARN — corp deviation',
        'WARN — foreign pattern',   'WARN — rate limit near',   'WARN — replica lag',
        'WARN — operator anomaly',  'WARN — entropy low',       'WARN — auth failures',
        'WARN — memory pressure',   'WARN — port saturation',   'WARN — beacon change',
        'WARN — key rotation due',  'WARN — zone access freq',  'WARN — crest wear score',
        'WARN — hkb fluid low',     'WARN — net tunnel flap',   'WARN — db stats stale',
        'NOTICE — process flagged', 'NOTICE — unusual traffic', 'NOTICE — policy deviation',
        'NOTICE — shadow process',  'NOTICE — off-hours access','NOTICE — location mismatch',
        'NOTICE — glmz gate usage', 'NOTICE — schedule deviate',
        // 30 more
        'WARN — rbs disc wear',        'WARN — bci ring2 noise',      'WARN — hkb spring tension',
        'WARN — enclave attestation',  'WARN — glmz node lag',        'WARN — corp sync drift',
        'WARN — token near expiry',    'WARN — relay packet loss',    'WARN — db write amplify',
        'WARN — file descriptor high', 'WARN — cpu throttle 88°C',    'WARN — swap pressure',
        'WARN — tls cert chain depth', 'WARN — bci motor drift',      'WARN — dark-node latency',
        'WARN — audit log behind',     'WARN — snapshot stale 48h',   'WARN — license near limit',
        'WARN — rng entropy 32bit',    'WARN — bci power 12%',        'WARN — ipc queue depth',
        'WARN — hkb cycle count high', 'WARN — glmz enforcement near','WARN — corp data horizon',
        'NOTICE — corp-enclave query', 'NOTICE — off-site login',     'NOTICE — bci key age 180d',
        'NOTICE — rate limit 90%',     'NOTICE — new district node',  'NOTICE — freelancer blacklist',
    ];

    var WARN_MSGS = [
        'Operator 0x3301 accessed zone B\n7 events in 24h — baseline is 2',
        'BCI latency 18ms — threshold 10ms\nClassifier confidence 0.71 — watch',
        'Cert glmz.relay.internal: 7d expiry\nRenewal scheduled — no action yet',
        'Disk /var/bci at 85% capacity\nSchedule cleanup — 2h to critical',
        'Replication lag 4.4s — watch\nPrimary write rate elevated 3×',
        'Entropy pool: 48 bits — low\nReseeding from RDRAND — ok for now',
        'HKB wear_score=0.72 — service soon\n88 cycles to recommended interval',
        'BCI drift: 0.8μV/min on ring2\nBelow alarm threshold — log started',
        'Operator 0x7712 off-hours access\n03:22 district4 — within policy',
        'Net beacon 0xBE04 freq change\n+12kHz shift — interference or move?',
        'Corp audit: 4 events out-of-order\nTimestamps within 200ms — clock skew?',
        'Auth failures uid=3301: 3 in 1h\nAbove baseline 1 — no lockout yet',
        'Memory RSS 1.4G — 73% of limit\nGrowth 44MB/h — check for leak',
        'glmz gate 12 use 44× today\nBaseline 12× — freelancer surge?',
        'Shadow process pid=31337 found\nNot in manifest — investigating',
        'Location mismatch: 0x4492\nLast seen d7, login from d12',
        'Corp policy deviation: uid=0x3301\n3 zone-B accesses without prior auth',
        'Foreign key pattern in BCI stream\nClassifier flagging unknown operator',
        'Key rotation 12d overdue: corp-vault\nSchedule window before expiry',
        'RBS thermal 47C — within spec\nElev. above baseline 38C — log',
        'HKB hydraulic: fluid 20% remaining\nRefill due before next session',
        'Net tunnel wg to dist4 flapped 3×\nStability: 94% uptime — watch',
        'DB stats 180s stale: bci_events\nPlanner using old data — ANALYZE',
        'Cert corp-enclave depth=1: 14d\nAuto-renew queued — monitor',
        'BCI ring3 impedance creeping up\n18kΩ — trend toward 22kΩ limit',
        // 30 more
        'RBS disc wear-score 0.61 — service at 0.80\n14,200 rotations logged this quarter',
        'BCI ring2 noise floor elevated\nBaseline 0.8μV → now 1.4μV — possible impedance',
        'HKB spring tension 87% spec\nFall below 80% triggers manual inspect',
        'Enclave attestation skew 3.2s\nClock drift — resync before next audit',
        'GLMZ node dist12 latency 88ms\nBaseline 22ms — check relay path',
        'Corp sync drift: 9 events behind\nPrimary load elevated — catch-up queued',
        'Auth token expiry in 4h: uid=0x4492\nRenewal queued — no action needed yet',
        'Relay packet loss 0.8%: dist9→dist12\nAbove baseline 0.1% — path degraded',
        'DB write amplification 4.8×\nLSM compaction behind — schedule window',
        'File descriptors 1800/2048\nApproaching limit — check for leaks',
        'CPU core3 throttling at 88°C\nCooling nominal — sustained load spike',
        'Swap pressure 14% — watch\nRSS trending up, GC may not keep pace',
        'TLS cert chain depth=4: corp-root\nUnusual — verify issuer hierarchy',
        'BCI motor cortex drift 0.6%/hr\nBelow abort threshold — calibrate next session',
        'Dark-node latency 340ms: glmz/7\nNominal <80ms — relay path congested',
        'Audit log 44s behind real-time\nWriter blocked on disk flush — ok for now',
        'Snapshot 48h stale: bci_events\nScheduled refresh missed — retry queued',
        'License utilization 88%: bci_nodes\n12 seats remain — notify procurement',
        'RNG entropy pool 32 bits\nBelow 64-bit threshold — kernel reseed pending',
        'BCI power reserve 12%\nBattery degradation flag — replace before next op',
        'IPC queue depth 420: bci_daemon\nNominal 50 — consumer falling behind',
        'HKB cycle count 9,800 — service at 10k\nSchedule maintenance before next session',
        'Corp enforcement notice: zone-C access\nOperator 0x3301 flagged — within policy',
        'Corp data retention horizon breach\nEvents older than 90d present — archive',
        'Corp enclave query volume spike\n3× baseline in last hour — investigate',
        'Off-site login: uid=0x7712 dist4\nLast known location: dist9 — review',
        'BCI key age 180d: uid=0x3301\nRotation overdue — schedule in 14d window',
        'Rate limit 90%: relay forwarding\nCorp quota — request increase or throttle',
        'New district node dist2/fab-9 online\nNot in routing table — manual review',
        'Freelancer 0x4492 on watchlist match\nLow confidence — monitor, no action yet',
    ];

    // ── Glyph character pool ────────────────────────────────────────────────
    var GLYPH_CHARS =
        '░▒▓█▌▐▀▄■□▪▫◆◇○●◉◎⊗⊕⊙∅' +
        '∞≠≈∫∂∆Ωπμλφψξζ' +
        '⌂⌀⌘⌬⌭⌫' +
        '✦✧✩✫✭✯✱✲✳✴✵✶✷✸✹✺✻✼' +
        '⬡⬢⬠⬟⬜⬝' +
        '⠿⠻⠷⠾⠽⠯⠫⠳' +
        '₿€¥₩₹Φ₽₼₺₴₦' +
        'アイウエオカキクケコサシスセソタチツテトナニヌネノ' +
        'ΑΒΓΔΕΖΗΘΙΚΛΜΝΞΟΠΡΣΤΥΦΧΨΩαβγδεζηθικλμνξοπρστυφχψω' +
        '龍炎風水土金木火' +
        '가나다라마바사아자차카타파하갈람밤삼잠참캄탐팜함';

    // ── Geo-window element report lines ────────────────────────────────────
    var GEO_REPORT_LINES = [
        'Atomic Mass:   [REDACTED]',
        'Isotope Δ:     +0.00441 amu',
        'Valence:       4f¹⁴5d¹⁰6p²',
        'Melting Pt:    3,817°K ± 0.3',
        'Boiling Pt:    [UNSTABLE]',
        'Half-life:     τ = 4.41×10⁻⁷s',
        'Nuclear Spin:  7/2',
        'Isomer State:  m2 confirmed',
        'Bind Energy:   8.812 MeV/nuc',
        'Cross Sect:    σ = 3.301 barn',
        'Decay Chain:   α→β⁻→γ cascade',
        'Oxidation:     +3, +5, +7',
        'Crystal Sys:   Orthorhombic',
        'Density:       19.77 g/cm³',
        'Resistivity:   2.4×10⁻⁸ Ω·m',
        'Band Gap:      0.441 eV',
        'Fermi Level:   4.92 eV',
        'Curie Temp:    1,187 K',
        'Neel Temp:     [classified]',
        'X-Ray Kα:      88.12 keV',
        'Emittance ε:   0.031',
        'Work Fnct φ:   4.41 eV',
        'Plasma ωₚ:     8.8×10¹⁵ Hz',
        'Refract n:     3.301+0.441i',
        'Reflectance:   R = 0.77',
        'Thermal K:     44.1 W/(m·K)',
        'Heat Cp:       28.12 J/(mol·K)',
        'Compressib:    3.3×10⁻¹¹ Pa⁻¹',
        'Synth Method:  Heavy-ion fusion',
        'Discovery:     GLMZ Deep Lab',
        'IUPAC Name:    Unennilium (Φ)',
        'Alt. Name:     [REDACTED]',
        'Registry:      Ψ-441-GLMZ-Φ',
        'Status:        RESTRICTED',
        'Clearance:     CORP EYES ONLY',
        'Sample Mass:   0.0000441 μg',
        'Stability:     [CLASSIFIED]',
        'Yield:         3.301×10⁻¹⁴ %',
        'Hazard:        EXTREME',
        'Manifold:      R⁴ compact',
        'Curvature κ:   +0.00441',
        'Euler χ:       2 (sphere)',
        'Genus:         0 orientable',
        'Betti b₀:      1',
        'Symm Group:    Iₕ (order 120)',
        'Dihedral:      A₅ × Z₂',
        'Vertex deg:    3-regular',
        'Face type:     pentagonal',
        'Dual solid:    icosahedron',
        'Conway:        D(I) notation',
        'Wythoff:       3|2 5',
        'Schläfli:      {5,3}',
        'Vol/Area:      V/A = 0.332',
        'Circum-R:      1.401 norm',
        'Inrad-r:       1.113 norm',
        'Midrad ρ:      1.309 norm',
        'Petrie Poly:   decagon',
        'Dual Verts:    12',
        'Coord Ring:    ℝ[x,y,z]/I₅',
        'Packing η:     0.9069 max',
        'Lattice:       FCC equiv',
        'Point Group:   Oh (48 ops)',
        'Space Group:   Fm3̄m',
        'Fourier k:     2π/a = 1.772',
        'Mode ω₀:       [CLASSIFIED]',
        'Signal SNR:    38.8 dB',
        'Phase δ:       0.441 rad',
        'Resonance:     4,410 Hz',
        'Impedance:     441 + 88i Ω',
    ];

    var tickTimer = null;

    function rand(a, b) { return Math.floor(Math.random() * (b - a + 1)) + a; }
    function pick(arr)  { return arr[rand(0, arr.length - 1)]; }
    function getHost()  { return document.querySelector('.console-bg-host'); }

    // Returns the bottom edge of .board-grid as a % of viewport height, +2% buffer.
    // Spawn positions use this as their minimum top value so nothing overlaps tiles.
    // randTop: unrestricted — effects can spawn anywhere including over tiles
    function randTop(lo, hi) { return rand(lo, hi); }

    // Try 4 random positions, pick the one with the least overlap against existing large overlays.
    // estW/estH are pixel estimates of the element being spawned.
    function bestPos(host, estW, estH, xMin, xMax, yMin, yMax) {
        var hw = window.innerWidth, hh = window.innerHeight;
        var sel = host.querySelectorAll('.cbg-win,.cbg-err-popup,.cbg-warn-popup,.cbg-memo');
        var rects = [];
        for (var i = 0; i < sel.length; i++) rects.push(sel[i].getBoundingClientRect());
        var bx = rand(xMin, xMax), by = rand(yMin, yMax), bov = Infinity;
        for (var t = 0; t < 4; t++) {
            var cx = rand(xMin, xMax), cy = rand(yMin, yMax);
            var px = (cx / 100) * hw, py = (cy / 100) * hh;
            var ov = 0;
            for (var r = 0; r < rects.length; r++) {
                var rc = rects[r];
                var ox = Math.max(0, Math.min(px + estW, rc.right)  - Math.max(px, rc.left));
                var oy = Math.max(0, Math.min(py + estH, rc.bottom) - Math.max(py, rc.top));
                ov += ox * oy;
            }
            if (ov < bov) { bov = ov; bx = cx; by = cy; }
        }
        return [bx, by];
    }

    // ── Terminal windows ────────────────────────────────────────────────────

    function spawnWindow(extraDelay, posX, posY, extraClass) {
        setTimeout(function () {
            var host = getHost();
            if (!host) return;

            // ~20% chance the window lingers, waiting for input
            var waiting = Math.random() < 0.20;

            var win = document.createElement('div');
            var colorVar = pick(['', '', 'cbg-win--blue', 'cbg-win--amber']);
            win.className = 'cbg-win' + (colorVar ? ' ' + colorVar : '') + (extraClass ? ' ' + extraClass : '');
            var wp = posX !== undefined ? [posX, posY] : bestPos(host, 228, 140, -8, 88, 4, 76);
            win.style.left = wp[0] + '%';
            win.style.top  = wp[1] + '%';

            var titleEl = document.createElement('div');
            titleEl.className = 'cbg-title';
            titleEl.textContent = pick(TITLES);
            win.appendChild(titleEl);

            var body = document.createElement('div');
            body.className = 'cbg-body';
            win.appendChild(body);

            host.appendChild(win);

            var lineCount = rand(2, 6);
            var lines = [];
            for (var i = 0; i < lineCount; i++) { lines.push(pick(LINES)); }

            var success = !waiting && Math.random() > 0.32;
            var result  = pick(success ? OK_RESULTS : ERR_RESULTS);
            var resCls  = success ? 'cbg-ok' : 'cbg-err';

            var idx = 0;
            var lineTimer = setInterval(function () {
                if (idx < lines.length) {
                    var ln = document.createElement('div');
                    ln.className = 'cbg-line';
                    ln.textContent = lines[idx];
                    body.appendChild(ln);
                    idx++;
                } else {
                    clearInterval(lineTimer);
                    setTimeout(function () {
                        if (waiting) {
                            // Show a prompt line with blinking cursor — no result, lingers
                            var prompt = document.createElement('div');
                            prompt.className = 'cbg-line cbg-prompt';
                            prompt.innerHTML = '> <span class="cbg-cursor">&#9608;</span>';
                            body.appendChild(prompt);
                            // Long random TTL: 6–18 seconds
                            setTimeout(function () {
                                win.classList.add('cbg-win--out');
                                setTimeout(function () {
                                    if (win.parentNode) win.parentNode.removeChild(win);
                                }, 160);
                            }, rand(6000, 18000));
                        } else {
                            var res = document.createElement('div');
                            res.className = 'cbg-line ' + resCls;
                            res.textContent = result;
                            body.appendChild(res);

                            setTimeout(function () {
                                win.classList.add('cbg-win--out');
                                setTimeout(function () {
                                    if (win.parentNode) win.parentNode.removeChild(win);
                                }, 160);
                            }, rand(700, 2500));
                        }
                    }, 130);
                }
            }, rand(4, 12));
        }, extraDelay || 0);
    }

    function spawnCascade() {
        var n = rand(3, 6);
        var x = rand(-5, 75), y = randTop(3, 85);
        var stepX = rand(2, 5), stepY = rand(1, 4);
        for (var i = 0; i < n; i++) {
            spawnWindow(i * rand(65, 210), x + i * stepX, y + i * stepY, 'cbg-cascade');
        }
    }

    // ── Fatal error popup ───────────────────────────────────────────────────

    function spawnError(posX, posY) {
        var host = getHost();
        if (!host) return;

        var popup = document.createElement('div');
        popup.className = 'cbg-err-popup';
        var ep = posX !== undefined ? [posX, posY] : bestPos(host, 310, 90, -5, 80, 5, 88);
        popup.style.left = ep[0] + '%';
        popup.style.top  = ep[1] + '%';

        // Layout: [red icon] | [title \n message \n ... \n OK btn]
        var icon = document.createElement('div');
        icon.className = 'cbg-err-popup-icon';
        icon.textContent = '⬛';   // replaced visually via CSS — just a block placeholder
        popup.appendChild(icon);

        var content = document.createElement('div');
        content.className = 'cbg-err-popup-content';

        var titleEl = document.createElement('div');
        titleEl.className = 'cbg-err-popup-title';
        titleEl.textContent = pick(FATAL_TITLES);
        content.appendChild(titleEl);

        var msgEl = document.createElement('div');
        msgEl.className = 'cbg-err-popup-msg';
        msgEl.textContent = pick(FATAL_MSGS);
        content.appendChild(msgEl);

        popup.appendChild(content);

        host.appendChild(popup);

        setTimeout(function () {
            popup.classList.add('cbg-win--out');
            setTimeout(function () {
                if (popup.parentNode) popup.parentNode.removeChild(popup);
            }, 160);
        }, rand(1800, 4000));
    }

    // ── Warning popup ───────────────────────────────────────────────────────

    function spawnWarning(posX, posY) {
        var host = getHost();
        if (!host) return;

        var popup = document.createElement('div');
        popup.className = 'cbg-warn-popup';
        var wp2 = posX !== undefined ? [posX, posY] : bestPos(host, 290, 90, -5, 82, 5, 88);
        popup.style.left = wp2[0] + '%';
        popup.style.top  = wp2[1] + '%';

        var content = document.createElement('div');
        content.className = 'cbg-warn-popup-content';

        var titleEl = document.createElement('div');
        titleEl.className = 'cbg-warn-popup-title';
        titleEl.textContent = pick(WARN_TITLES);
        content.appendChild(titleEl);

        var msgEl = document.createElement('div');
        msgEl.className = 'cbg-warn-popup-msg';
        msgEl.textContent = pick(WARN_MSGS);
        content.appendChild(msgEl);

        popup.appendChild(content);
        host.appendChild(popup);

        setTimeout(function () {
            popup.classList.add('cbg-win--out');
            setTimeout(function () {
                if (popup.parentNode) popup.parentNode.removeChild(popup);
            }, 160);
        }, rand(2500, 5500));
    }

    // ── Floating code fragments ─────────────────────────────────────────────

    function spawnFrag() {
        var host = getHost();
        if (!host) return;

        var el = document.createElement('div');
        el.className = 'cbg-frag';
        el.style.left = rand(-4, 94) + '%';
        el.style.top  = randTop(2, 94) + '%';
        host.appendChild(el);

        var text = pick(FRAGS);
        var idx  = 0;
        var cur  = '';

        var typeTimer = setInterval(function () {
            if (idx >= text.length) {
                clearInterval(typeTimer);
                setTimeout(function () {
                    el.classList.add('cbg-frag--out');
                    setTimeout(function () {
                        if (el.parentNode) el.parentNode.removeChild(el);
                    }, 360);
                }, rand(18, 75));
                return;
            }
            cur += text[idx++];
            el.textContent = cur;
        }, rand(1, 3));
    }

    // ── Geometry schematic window ────────────────────────────────────────────

    var PHI = (1 + Math.sqrt(5)) / 2;
    var GEO_SHAPES = {
        tetra:  { label:'tetrahedron',  verts:[[1,1,1],[-1,-1,1],[-1,1,-1],[1,-1,-1]], edges:[[0,1],[0,2],[0,3],[1,2],[1,3],[2,3]] },
        cube:   { label:'hexahedron',   verts:[[-1,-1,-1],[1,-1,-1],[1,1,-1],[-1,1,-1],[-1,-1,1],[1,-1,1],[1,1,1],[-1,1,1]], edges:[[0,1],[1,2],[2,3],[3,0],[4,5],[5,6],[6,7],[7,4],[0,4],[1,5],[2,6],[3,7]] },
        octa:   { label:'octahedron',   verts:[[0,1,0],[0,-1,0],[1,0,0],[-1,0,0],[0,0,1],[0,0,-1]], edges:[[0,2],[0,3],[0,4],[0,5],[1,2],[1,3],[1,4],[1,5],[2,4],[4,3],[3,5],[5,2]] },
        icosa:    { label:'icosahedron',     verts:[[0,1,PHI],[0,-1,PHI],[0,1,-PHI],[0,-1,-PHI],[1,PHI,0],[-1,PHI,0],[1,-PHI,0],[-1,-PHI,0],[PHI,0,1],[-PHI,0,1],[PHI,0,-1],[-PHI,0,-1]], edges:[[0,1],[0,4],[0,5],[0,8],[0,9],[1,6],[1,7],[1,8],[1,9],[2,3],[2,4],[2,5],[2,10],[2,11],[3,6],[3,7],[3,10],[3,11],[4,5],[4,8],[4,10],[5,9],[5,11],[6,7],[6,8],[6,10],[7,9],[7,11],[8,10],[9,11]] },
        prism:    { label:'triangular prism',  verts:[[0,1,1],[0.866,-0.5,1],[-0.866,-0.5,1],[0,1,-1],[0.866,-0.5,-1],[-0.866,-0.5,-1]], edges:[[0,1],[1,2],[2,0],[3,4],[4,5],[5,3],[0,3],[1,4],[2,5]] },
        stella:   { label:'stella octangula',  verts:[[1,1,1],[-1,-1,1],[-1,1,-1],[1,-1,-1],[-1,-1,-1],[1,1,-1],[1,-1,1],[-1,1,1]], edges:[[0,1],[0,2],[0,3],[1,2],[1,3],[2,3],[4,5],[4,6],[4,7],[5,6],[5,7],[6,7]] },
        cubocta:  { label:'cuboctahedron',     verts:[[1,1,0],[-1,1,0],[1,-1,0],[-1,-1,0],[1,0,1],[-1,0,1],[1,0,-1],[-1,0,-1],[0,1,1],[0,-1,1],[0,1,-1],[0,-1,-1]], edges:[[0,4],[0,6],[0,8],[0,10],[1,5],[1,7],[1,8],[1,10],[2,4],[2,6],[2,9],[2,11],[3,5],[3,7],[3,9],[3,11],[4,8],[4,9],[5,8],[5,9],[6,10],[6,11],[7,10],[7,11]] },
        antiprism:{ label:'square antiprism',  verts:[[1,1,0],[0,1,1],[-1,1,0],[0,1,-1],[0.707,-1,0.707],[-0.707,-1,0.707],[-0.707,-1,-0.707],[0.707,-1,-0.707]], edges:[[0,1],[1,2],[2,3],[3,0],[4,5],[5,6],[6,7],[7,4],[0,4],[0,7],[1,4],[1,5],[2,5],[2,6],[3,6],[3,7]] },
        pyramid:  { label:'square pyramid',    verts:[[0,1.2,0],[1,-0.5,1],[-1,-0.5,1],[-1,-0.5,-1],[1,-0.5,-1]], edges:[[0,1],[0,2],[0,3],[0,4],[1,2],[2,3],[3,4],[4,1]] },
        pentaprism:{ label:'pentagonal prism', verts:[[1,1,0],[0.309,1,0.951],[-0.809,1,0.588],[-0.809,1,-0.588],[0.309,1,-0.951],[1,-1,0],[0.309,-1,0.951],[-0.809,-1,0.588],[-0.809,-1,-0.588],[0.309,-1,-0.951]], edges:[[0,1],[1,2],[2,3],[3,4],[4,0],[5,6],[6,7],[7,8],[8,9],[9,5],[0,5],[1,6],[2,7],[3,8],[4,9]] },
        dodeca:   { label:'dodecahedron',      verts:[[1,1,1],[1,1,-1],[1,-1,1],[1,-1,-1],[-1,1,1],[-1,1,-1],[-1,-1,1],[-1,-1,-1],[0,0.618,1.618],[0,0.618,-1.618],[0,-0.618,1.618],[0,-0.618,-1.618],[0.618,1.618,0],[0.618,-1.618,0],[-0.618,1.618,0],[-0.618,-1.618,0],[1.618,0,0.618],[1.618,0,-0.618],[-1.618,0,0.618],[-1.618,0,-0.618]], edges:[[0,8],[0,12],[0,16],[1,9],[1,12],[1,17],[2,10],[2,13],[2,16],[3,11],[3,13],[3,17],[4,8],[4,14],[4,18],[5,9],[5,14],[5,19],[6,10],[6,15],[6,18],[7,11],[7,15],[7,19],[8,10],[9,11],[12,14],[13,15],[16,17],[18,19]] }
    };
    var GEO_KEYS = ['tetra','cube','octa','icosa','flower','metatron','prism','stella','cubocta','antiprism','vesica','spiral','lissajous','star5','torus','helix','dodeca','pyramid','pentaprism','rose','cardioid','asteroid','epicycloid','web'];

    function spawnGeoWindow() {
        var host = getHost();
        if (!host) return;
        var key = pick(GEO_KEYS);
        var shape = GEO_SHAPES[key];

        var win = document.createElement('div');
        win.className = 'cbg-win cbg-geo-win';
        var gp = bestPos(host, 240, 130, -5, 80, 5, 90);
        win.style.left = gp[0] + '%';
        win.style.top  = gp[1] + '%';

        var titleEl = document.createElement('div');
        titleEl.className = 'cbg-title';
        titleEl.textContent = 'schematic/' + (shape ? shape.label : key);
        win.appendChild(titleEl);

        var S = 110;
        var bodyEl = document.createElement('div');
        bodyEl.className = 'cbg-geo-body';
        var cv = document.createElement('canvas');
        cv.className = 'cbg-geo-canvas';
        cv.width = S; cv.height = S;
        bodyEl.appendChild(cv);
        win.appendChild(bodyEl);
        host.appendChild(win);

        var ctx2 = cv.getContext('2d');
        var angle = Math.random() * Math.PI * 2;
        var rafId = null;

        function proj3(v, rx, ry) {
            var x = v[0], y = v[1], z = v[2];
            var cry = Math.cos(ry), sry = Math.sin(ry);
            var x1 = x * cry + z * sry, z1 = -x * sry + z * cry;
            var crx = Math.cos(rx), srx = Math.sin(rx);
            var y1 = y * crx - z1 * srx, z2 = y * srx + z1 * crx;
            var d = 6 / (6 + z2 * 0.5);
            return [x1 * d * 32, y1 * d * 32];
        }

        function drawWire(sh, rx, ry) {
            var pts = sh.verts.map(function(v){ return proj3(v, rx, ry); });
            ctx2.strokeStyle = 'rgba(70,210,170,0.55)'; ctx2.lineWidth = 0.7;
            sh.edges.forEach(function(e){ ctx2.beginPath(); ctx2.moveTo(pts[e[0]][0],pts[e[0]][1]); ctx2.lineTo(pts[e[1]][0],pts[e[1]][1]); ctx2.stroke(); });
            ctx2.fillStyle = 'rgba(70,210,170,0.85)';
            pts.forEach(function(p){ ctx2.beginPath(); ctx2.arc(p[0],p[1],1.2,0,Math.PI*2); ctx2.fill(); });
        }

        function drawFlower(t) {
            var r = 22;
            ctx2.strokeStyle = 'rgba(70,200,220,0.38)'; ctx2.lineWidth = 0.6;
            ctx2.beginPath(); ctx2.arc(0,0,r,0,Math.PI*2); ctx2.stroke();
            for(var i=0;i<6;i++){ var a=(i/6)*Math.PI*2+t*0.05; ctx2.beginPath(); ctx2.arc(Math.cos(a)*r,Math.sin(a)*r,r,0,Math.PI*2); ctx2.stroke(); }
            ctx2.strokeStyle='rgba(70,220,170,0.65)'; ctx2.lineWidth=0.8;
            ctx2.beginPath();
            for(var j=0;j<6;j++){ var a2=(j/6)*Math.PI*2+t*0.05; var p=[Math.cos(a2)*r,Math.sin(a2)*r]; if(j===0)ctx2.moveTo(p[0],p[1]); else ctx2.lineTo(p[0],p[1]); }
            ctx2.closePath(); ctx2.stroke();
        }

        function drawMetatron(t) {
            var r = 16, ir = r, or2 = r * 2;
            var cs = [[0,0]];
            for(var i=0;i<6;i++){ var a=(i/6)*Math.PI*2+t*0.03; cs.push([Math.cos(a)*ir,Math.sin(a)*ir]); }
            for(var i=0;i<6;i++){ var a=(i/6)*Math.PI*2+Math.PI/6+t*0.03; cs.push([Math.cos(a)*or2,Math.sin(a)*or2]); }
            ctx2.strokeStyle='rgba(70,180,220,0.28)'; ctx2.lineWidth=0.5;
            cs.forEach(function(c){ ctx2.beginPath(); ctx2.arc(c[0],c[1],r,0,Math.PI*2); ctx2.stroke(); });
            ctx2.strokeStyle='rgba(70,220,170,0.45)'; ctx2.lineWidth=0.5;
            for(var a=0;a<cs.length;a++) for(var b=a+1;b<cs.length;b++){ ctx2.beginPath(); ctx2.moveTo(cs[a][0],cs[a][1]); ctx2.lineTo(cs[b][0],cs[b][1]); ctx2.stroke(); }
            ctx2.fillStyle='rgba(70,220,170,0.7)';
            cs.forEach(function(c){ ctx2.beginPath(); ctx2.arc(c[0],c[1],1,0,Math.PI*2); ctx2.fill(); });
        }

        function drawVesica(t) {
            var r=24, d=r*0.6;
            ctx2.save(); ctx2.rotate(t*0.02);
            ctx2.strokeStyle='rgba(70,200,220,0.45)'; ctx2.lineWidth=0.7;
            ctx2.beginPath(); ctx2.arc(-d/2,0,r,0,Math.PI*2); ctx2.stroke();
            ctx2.beginPath(); ctx2.arc(d/2,0,r,0,Math.PI*2); ctx2.stroke();
            ctx2.strokeStyle='rgba(70,220,170,0.75)'; ctx2.lineWidth=0.8;
            var half=Math.acos(d/(2*r));
            ctx2.beginPath(); ctx2.arc(-d/2,0,r,-half,half); ctx2.arc(d/2,0,r,Math.PI-half,Math.PI+half); ctx2.closePath(); ctx2.stroke();
            ctx2.restore();
        }
        function drawSpiral(t) {
            var maxR=38, turns=4;
            ctx2.strokeStyle='rgba(70,210,170,0.65)'; ctx2.lineWidth=0.7;
            ctx2.beginPath();
            for(var i=0;i<=300;i++){ var f=i/300; var a=f*turns*Math.PI*2+t*0.05; var r=f*maxR; if(i===0)ctx2.moveTo(Math.cos(a)*r,Math.sin(a)*r); else ctx2.lineTo(Math.cos(a)*r,Math.sin(a)*r); }
            ctx2.stroke();
            ctx2.strokeStyle='rgba(70,190,220,0.35)'; ctx2.lineWidth=0.5;
            ctx2.beginPath();
            for(var i=0;i<=300;i++){ var f=i/300; var a=f*turns*Math.PI*2+t*0.05+Math.PI; var r=f*maxR; if(i===0)ctx2.moveTo(Math.cos(a)*r,Math.sin(a)*r); else ctx2.lineTo(Math.cos(a)*r,Math.sin(a)*r); }
            ctx2.stroke();
        }
        function drawLissajous(t) {
            var a=3, b=2, R=38;
            ctx2.strokeStyle='rgba(70,210,170,0.60)'; ctx2.lineWidth=0.8;
            ctx2.beginPath();
            for(var i=0;i<=400;i++){ var phi=(i/400)*Math.PI*2; var x=R*Math.sin(a*phi+t*0.04); var y=R*Math.sin(b*phi); if(i===0)ctx2.moveTo(x,y); else ctx2.lineTo(x,y); }
            ctx2.stroke();
        }
        function drawStar5(t) {
            var r=36, ir=14, pts=5, rot=t*0.04;
            ctx2.strokeStyle='rgba(70,210,170,0.55)'; ctx2.lineWidth=0.7;
            ctx2.beginPath();
            for(var i=0;i<=pts*2;i++){ var rad=(i%2===0)?r:ir; var a=(i/(pts*2))*Math.PI*2+rot-Math.PI/2; if(i===0)ctx2.moveTo(Math.cos(a)*rad,Math.sin(a)*rad); else ctx2.lineTo(Math.cos(a)*rad,Math.sin(a)*rad); }
            ctx2.closePath(); ctx2.stroke();
            ctx2.strokeStyle='rgba(70,200,220,0.38)'; ctx2.lineWidth=0.6;
            ctx2.beginPath();
            for(var i=0;i<=pts;i++){ var a=(i/pts)*Math.PI*2+rot-Math.PI/2; if(i===0)ctx2.moveTo(Math.cos(a)*ir,Math.sin(a)*ir); else ctx2.lineTo(Math.cos(a)*ir,Math.sin(a)*ir); }
            ctx2.closePath(); ctx2.stroke();
            var op=[]; for(var i=0;i<pts;i++){ var a=(i/pts)*Math.PI*2+rot-Math.PI/2; op.push([Math.cos(a)*r,Math.sin(a)*r]); }
            ctx2.strokeStyle='rgba(70,220,170,0.28)'; ctx2.lineWidth=0.5;
            for(var i=0;i<pts;i++){ var j=(i+2)%pts; ctx2.beginPath(); ctx2.moveTo(op[i][0],op[i][1]); ctx2.lineTo(op[j][0],op[j][1]); ctx2.stroke(); }
        }
        function drawTorus(t) {
            var R=22, r=10, rings=8;
            for(var i=0;i<rings;i++){ var phi=(i/rings)*Math.PI*2+t*0.03; var cx=Math.cos(phi)*R, cy=Math.sin(phi)*R*0.35; var sc=0.5+0.5*Math.abs(Math.cos(phi)); var al=(0.3+0.3*Math.abs(Math.cos(phi))).toFixed(2); ctx2.strokeStyle='rgba(70,210,170,'+al+')'; ctx2.lineWidth=0.6; ctx2.beginPath(); ctx2.ellipse(cx,cy,r*sc,r*0.35,phi,0,Math.PI*2); ctx2.stroke(); }
            ctx2.strokeStyle='rgba(70,200,220,0.45)'; ctx2.lineWidth=0.7;
            ctx2.beginPath(); ctx2.ellipse(0,0,R+r,(R+r)*0.35,0,0,Math.PI*2); ctx2.stroke();
            ctx2.beginPath(); ctx2.ellipse(0,0,R-r,(R-r)*0.35,0,0,Math.PI*2); ctx2.stroke();
        }
        function drawHelix(t) {
            var R=14, turns=3;
            ctx2.lineWidth=0.8;
            ctx2.strokeStyle='rgba(70,210,170,0.65)'; ctx2.beginPath();
            for(var i=0;i<=300;i++){ var f=i/300; var ang=f*turns*Math.PI*2+t*0.05; var y=f*56-28; if(i===0)ctx2.moveTo(Math.cos(ang)*R,y); else ctx2.lineTo(Math.cos(ang)*R,y); }
            ctx2.stroke();
            ctx2.strokeStyle='rgba(70,190,220,0.50)'; ctx2.beginPath();
            for(var i=0;i<=300;i++){ var f=i/300; var ang=f*turns*Math.PI*2+t*0.05+Math.PI; var y=f*56-28; if(i===0)ctx2.moveTo(Math.cos(ang)*R,y); else ctx2.lineTo(Math.cos(ang)*R,y); }
            ctx2.stroke();
            ctx2.strokeStyle='rgba(70,220,170,0.22)'; ctx2.lineWidth=0.5;
            for(var i=0;i<=300;i+=25){ var f=i/300; var ang=f*turns*Math.PI*2+t*0.05; var y=f*56-28; ctx2.beginPath(); ctx2.moveTo(Math.cos(ang)*R,y); ctx2.lineTo(Math.cos(ang+Math.PI)*R,y); ctx2.stroke(); }
        }

        function drawRose(t) {
            var R=38, k=3;
            ctx2.strokeStyle='rgba(70,210,170,0.70)'; ctx2.lineWidth=0.8;
            ctx2.beginPath();
            for(var i=0;i<=720;i++){
                var th=(i/720)*Math.PI*2;
                var r=R*Math.cos(k*(th+t*0.018));
                if(i===0) ctx2.moveTo(r*Math.cos(th),r*Math.sin(th));
                else ctx2.lineTo(r*Math.cos(th),r*Math.sin(th));
            }
            ctx2.stroke();
            ctx2.strokeStyle='rgba(70,200,220,0.22)'; ctx2.lineWidth=0.4;
            ctx2.beginPath(); ctx2.arc(0,0,R,0,Math.PI*2); ctx2.stroke();
        }
        function drawCardioid(t) {
            var a=18, rot=t*0.012;
            ctx2.strokeStyle='rgba(70,210,170,0.65)'; ctx2.lineWidth=0.8;
            ctx2.beginPath();
            for(var i=0;i<=360;i++){
                var th=(i/360)*Math.PI*2;
                var r=a*(1+Math.cos(th));
                if(i===0) ctx2.moveTo(r*Math.cos(th+rot),r*Math.sin(th+rot));
                else ctx2.lineTo(r*Math.cos(th+rot),r*Math.sin(th+rot));
            }
            ctx2.stroke();
            ctx2.strokeStyle='rgba(70,200,220,0.22)'; ctx2.lineWidth=0.5;
            ctx2.beginPath(); ctx2.arc(0,0,a*2,0,Math.PI*2); ctx2.stroke();
        }
        function drawAsteroid(t) {
            var R=36, rot=t*0.02;
            ctx2.strokeStyle='rgba(70,210,170,0.65)'; ctx2.lineWidth=0.8;
            ctx2.beginPath();
            for(var i=0;i<=360;i++){
                var p=(i/360)*Math.PI*2;
                var x=R*Math.pow(Math.cos(p+rot),3), y=R*Math.pow(Math.sin(p+rot),3);
                if(i===0) ctx2.moveTo(x,y); else ctx2.lineTo(x,y);
            }
            ctx2.stroke();
            ctx2.strokeStyle='rgba(70,200,220,0.25)'; ctx2.lineWidth=0.5;
            ctx2.beginPath(); ctx2.arc(0,0,R,0,Math.PI*2); ctx2.stroke();
            for(var i=0;i<4;i++){
                var a=(i/4)*Math.PI*2+rot;
                ctx2.strokeStyle='rgba(70,220,170,0.18)'; ctx2.lineWidth=0.4;
                ctx2.beginPath(); ctx2.moveTo(0,0); ctx2.lineTo(Math.cos(a)*R,Math.sin(a)*R); ctx2.stroke();
            }
        }
        function drawEpicycloid(t) {
            var Rc=24, rc=8, rot=t*0.015;
            ctx2.strokeStyle='rgba(70,210,170,0.65)'; ctx2.lineWidth=0.8;
            ctx2.beginPath();
            for(var i=0;i<=600;i++){
                var p=(i/600)*Math.PI*2+rot;
                var x=(Rc+rc)*Math.cos(p)-rc*Math.cos((Rc/rc+1)*p);
                var y=(Rc+rc)*Math.sin(p)-rc*Math.sin((Rc/rc+1)*p);
                if(i===0) ctx2.moveTo(x,y); else ctx2.lineTo(x,y);
            }
            ctx2.stroke();
            ctx2.strokeStyle='rgba(70,200,220,0.22)'; ctx2.lineWidth=0.5;
            ctx2.beginPath(); ctx2.arc(0,0,Rc,0,Math.PI*2); ctx2.stroke();
        }
        function drawWeb(t) {
            var rings=5, spokes=8, maxR=40, rot=t*0.01;
            ctx2.strokeStyle='rgba(70,200,220,0.35)'; ctx2.lineWidth=0.5;
            for(var s=0;s<spokes;s++){
                var a=(s/spokes)*Math.PI*2+rot;
                ctx2.beginPath(); ctx2.moveTo(0,0); ctx2.lineTo(Math.cos(a)*maxR,Math.sin(a)*maxR); ctx2.stroke();
            }
            ctx2.strokeStyle='rgba(70,210,170,0.55)'; ctx2.lineWidth=0.7;
            for(var ri=1;ri<=rings;ri++){
                var r=(ri/rings)*maxR;
                ctx2.beginPath();
                for(var s=0;s<=spokes;s++){
                    var a=(s/spokes)*Math.PI*2+rot;
                    if(s===0) ctx2.moveTo(Math.cos(a)*r,Math.sin(a)*r);
                    else ctx2.lineTo(Math.cos(a)*r,Math.sin(a)*r);
                }
                ctx2.closePath(); ctx2.stroke();
            }
        }

        function geoFrame() {
            ctx2.clearRect(0,0,S,S);
            ctx2.save(); ctx2.translate(S/2,S/2);
            angle += 0.007;
            if      (key==='flower')     drawFlower(angle);
            else if (key==='metatron')   drawMetatron(angle);
            else if (key==='vesica')     drawVesica(angle);
            else if (key==='spiral')     drawSpiral(angle);
            else if (key==='lissajous')  drawLissajous(angle);
            else if (key==='star5')      drawStar5(angle);
            else if (key==='torus')      drawTorus(angle);
            else if (key==='helix')      drawHelix(angle);
            else if (key==='rose')       drawRose(angle);
            else if (key==='cardioid')   drawCardioid(angle);
            else if (key==='asteroid')   drawAsteroid(angle);
            else if (key==='epicycloid') drawEpicycloid(angle);
            else if (key==='web')        drawWeb(angle);
            else                         drawWire(shape, angle*0.4, angle);
            ctx2.restore();
            rafId = requestAnimationFrame(geoFrame);
        }
        geoFrame();

        // ── Scrolling element report text (right of canvas) ────────────────
        var reportEl = document.createElement('div');
        reportEl.className = 'cbg-geo-report';
        var innerEl = document.createElement('div');
        innerEl.className = 'cbg-geo-report-inner';
        reportEl.appendChild(innerEl);
        bodyEl.appendChild(reportEl);

        var lineH = 9; // px per line at 0.34rem/1.4 leading
        var visCount = Math.ceil(110 / lineH) + 3;
        var reportBuf = [];
        for (var ri = 0; ri < visCount + 2; ri++) { reportBuf.push(pick(GEO_REPORT_LINES)); }
        innerEl.textContent = reportBuf.join('\n');
        var scrollOff = 0;
        var scrollTmr = setInterval(function () {
            scrollOff += 0.35;
            if (scrollOff >= lineH) {
                scrollOff -= lineH;
                reportBuf.shift();
                reportBuf.push(pick(GEO_REPORT_LINES));
                innerEl.textContent = reportBuf.join('\n');
            }
            innerEl.style.transform = 'translateY(' + scrollOff.toFixed(1) + 'px)';
        }, 60);

        var ttl = rand(4000, 9000);
        setTimeout(function () {
            clearInterval(scrollTmr);
            cancelAnimationFrame(rafId);
            win.classList.add('cbg-win--out');
            setTimeout(function () { if (win.parentNode) win.parentNode.removeChild(win); }, 500);
        }, ttl);
    }

    // ── Corporate memo intercept ──────────────────────────────────────────────

    var MEMOS = [
        'CLASSIFIED // EYES ONLY\nTO: VP Asset Security\nFROM: Board Dir. 94-C\nRE: Unit 7-W — Liability\n\nProceed w/ scheduled termination.\nFull blackout per Protocol 9.\nNo media. No record. Confirm 0300.\nAuth: 0x9f3a\n\n>>> GLMZ RELAY INTERCEPT <<<',
        'INTERNAL — DELETE AFTER READ\nTO: District Enforcement\nRE: Freelancer File #3301\n\nFlag for immediate termination.\nAssociates: Annex B.\nOffer suppress — THEN terminate.\nSilent bounty: Φ40,000 corp rate.\n\n>>> INTERCEPT :: dist12/relay <<<',
        'FROM: Crest Dynamics Legal\nTO: Subsidiary Compliance\nRE: Neural Data Harvest — Batch 7\n\nHarvest neural-sig per Q3 plan.\nConsent waiver buried in ToS §44-F.\nDo NOT flag to district oversight.\nDelete harvest logs after 72h.\n\n[RELAY CORRUPTION — partial recv]',
        'PRIORITY: URGENT\nTO: Security Chief, Dist 9\nRE: Witness Mgmt — Case #0091\n\nAll three witnesses — silence them.\nPreferred: reassignment.\nFallback: Protocol Null.\nContractor already briefed.\n\n[SIG: 0xdc3545 // VAULTDROP]',
        'FROM: Acquisition Strategy\nTO: Field Operations\nRE: Sector 4 Hostile Takeover\n\nPhase 1: Destroy competitor supply.\nPhase 2: Corner remaining market.\nPhase 3: Price floor +400%.\nKeep district enforcement on payroll.\n\n>>> DARK NODE :: glmz/7 <<<',
        'TO: Behavioral Analytics Team\nRE: Pop. Compliance — Batch 9\n\nDeploy sublim. seq. in dist2 feed.\nTarget: dissent suppression.\nVector: entertainment network.\nDeny if queried. Log: none.\n\n[INTERCEPT CONFIDENCE: 0.88]',
        'PRIVILEGED COMMUNICATION\nFROM: Acquisitions Div. 3\nTO: Field Security\nRE: Competitor Asset — Terminal\n\nSubject refused acquisition offer.\nAuthorize final resolution.\nRecover IP before cleanup.\nRoute via dark node only.\n\n[GLMZ INTERCEPT: 94%]',
        'FROM: Behavioral Mod. Group\nTO: Neural Interface Program\nRE: Opt-Out Handling — Priority\n\nOperators flagging consent removal:\nDo NOT honor. Flag as compromised.\nAccelerate neural-key binding.\nLegal has pre-approved language.\n\n>>> CREST DYNAMICS INTERNAL <<<',
        'URGENT — ALL DISTRICT COMMANDERS\nFROM: Enforcement Central\nRE: Freelancer Surge — District 7\n\nSurge is cover. Target is #3301.\nCollateral: acceptable up to 40%.\nNo press. No GLMZ incident report.\nClose window before 0600.\n\n[AUTH: 0xBE0441]',
        'FROM: Data Harvesting Unit 9\nTO: Board — Eyes Only\nRE: Q3 Neural Data — Profit\n\n44,000 operator profiles sold.\nBuyer: NeuralState consortium.\nData stripped of ID — plausibly.\nReturn: Φ4.4M corp rate.\n\n>>> RELAY CORRUPTION <<<',
        'TO: Corp Liaison, District 9\nFROM: Legal Stratagem\nRE: Liability Suppression\n\nSeven incident reports — suppress.\nSettle: Φ3,000 each, no admission.\nIf refused: standard protocol 7.\nDestroy original filings after.\n\n[INTERCEPT: dist9/dark-node]',
        'FROM: Medical Ethics Bypass\nTO: Cyberware Division\nRE: Non-Consenting Subjects\n\nTrial cohort 44 is involuntary.\nClinical oversight circumvented.\nOutcomes trending positive.\nTerminate failed cohort quietly.\n\n>>> DARK NODE :: dist4 <<<',
        'CLASSIFIED — BOARD LEVEL ONLY\nFROM: Asset Liquidation\nTO: Director 94-C\nRE: Witness List — Case #0099\n\nSix witnesses. Five located.\nSchedule: sequential, 72h window.\nMake them look accidental.\nFinal — offer Φ80k, else same.\n\n[SIG: 0x9A3301 // VERIFIED]',
        'FROM: Media Relations (Covert)\nTO: Entertainment Network\nRE: Narrative Seeding — Phase 3\n\nInsert: freelancers = terrorists.\nEnforcement = civic guardians.\nSubtlety required — 6mo campaign.\nDeny corp authorship at all costs.\n\n[RELAY: glmz-comms-d12]',
        'TO: Corp Security, District 4\nFROM: Territorial Division\nRE: Hostile Freelancer — 0x4492\n\nOperator has evidence of Q3 harvest.\nContain before they reach press.\nLevel 3 protocol authorized.\nNo record of this transmission.\n\n>>> RELAY CORRUPTION — partial <<<',
        'FROM: Population Control Div.\nTO: GLMZ District Administrators\nRE: Food Access Throttle — Batch 4\n\nDistrict 9 rationing at 44%.\nReduction to 30% approved Q1.\nFrame as supply chain failure.\nTrack compliance via BCI metrics.\n\n[INTERCEPT CONFIDENCE: 0.91]',
        'INTERNAL — DESTROY AFTER READ\nFROM: Corp Ethics Committee\nTO: [REDACTED]\nRE: Upcoming Ethics Review\n\nAnswers to inquiries 3, 7, 12:\n— Data: "aggregated, anonymized"\n— Consent: "implied via ToS"\n— Deaths: "within projections"\nMembers have been briefed.\n\n[SIG: 0xDEAD4412]',
        // 15 more
        'FROM: Extraction Unit 7\nTO: Field Ops Director\nRE: Asset 3301 — Status Update\n\nAsset uncooperative after 72h.\nStandard persuasion ineffective.\nPhase 2 authorized by Dir. 94-C.\nDispose after extraction complete.\n\n>>> GLMZ DARK NODE :: dist4 <<<',
        'FROM: Neural Analytics Board\nTO: BCI Program Dir.\nRE: Operator 0x4492 — Classify\n\nNeural profile matches dissident tag.\nRecommend silent reclassification.\nAccess throttle: 40% — covert.\nDo not inform subject.\n\n[RELAY: sec/vault-7]',
        'CONFIDENTIAL — NO EXTERNAL\nFROM: Subsidiary Relations\nTO: Enforcement Liaison\nRE: Competitor Infrastructure\n\nThree relay nodes confirmed hostile.\nCoordinate with dist9 enforcement.\nPlausible denial required.\nNo Crest equipment — freelancers.\n\n>>> INTERCEPT :: corp/mirror <<<',
        'TO: Narrative Ops Team\nFROM: Social Influence Div.\nRE: Freelancer Problem — Framing\n\nCurrent narrative: economic threat.\nProposed pivot: public safety.\nTimeline: 3-week push via ent/net.\nSuccess metric: 60% public favor.\n\n[GLMZ RELAY: dist12/comms]',
        'INTERNAL ONLY — LEGAL\nFROM: Compliance Division\nTO: Security Dir.\nRE: Incident 0091 — Paperwork\n\nThree deaths out of scope.\nFile as industrial accident.\nFamily settlements: Φ8,000 each.\nNDA required — enforce aggressively.\n\n[SIG: 0x9A3301 // CORP-LEGAL]',
        'FROM: Behavioral Modification R&D\nTO: Program Board\nRE: Trial Cohort 7 — Outcomes\n\nCompliance rate: 88% (target 80%).\nSubjects unaware of BCI seeding.\nSide effects: within tolerance.\nProceed to Batch 8 — 500 subjects.\n\n>>> DARK NODE :: dist7/relay <<<',
        'URGENT PRIORITY\nFROM: Intelligence Operations\nTO: Crest Dynamics\nRE: Journalist — Case #0441\n\nJournalist has partial Q3 data.\nSourced from inside — find leak.\nContain story before press cycle.\nPermanent solution if necessary.\n\n[INTERCEPT CONFIDENCE: 0.96]',
        'FROM: Territorial Expansion\nTO: Legal + Enforcement\nRE: District 2 Consolidation\n\nPhase 1 complete: 3 orgs dissolved.\nPhase 2: Purchase remaining assets.\nPhase 3: Restructure workforce.\nExpected redundancies: 400-600.\n\n>>> CORP/BROKER :: VAULTDROP <<<',
        'CLASSIFIED — ABOVE TOP\nFROM: AI Autonomy Division\nTO: Board Only\nRE: Behemoth Meridian-88 — Update\n\nAutonomy module fully deployed.\nHuman oversight: symbolic only.\nContingency removal: scheduled.\nDo not log this meeting.\n\n[SIG: 0xFF0000 // PURGE-ON-READ]',
        'TO: Forensic Suppression Team\nFROM: Director 94-C\nRE: Evidence — Batch 0099\n\nSeven files. Delete originals.\nOverwrite media 3 passes.\nPurge relay cache: dist4, dist7.\nConfirm by 0300 UTC.\n\n>>> RELAY CORRUPTION — terminal <<<',
        'FROM: Public Health Proxy\nTO: District Supply Chain\nRE: Pharmaceutical Diversion\n\nDivert Batch 44 to compliance stream.\nReduce district 9 access 60%.\nFrame as shortage — corp approved.\nProfits to hidden account 0x9F3A.\n\n[INTERCEPT: glmz/gate-12]',
        'INTERNAL — NO EXTERNAL\nFROM: BCI Surveillance Unit\nTO: Corp Intelligence\nRE: Watchlist Update — Q2\n\n3,301 operators under passive monitor.\n412 flagged for attention.\nNeural-key patterns attached.\nAutomated escalation if triggered.\n\n>>> CREST INTERNAL :: sec/enclave <<<',
        'TO: Field Security, District 12\nFROM: Asset Protection\nRE: Freelancer Collective — Action\n\nCell identified: 4 members, dark-node.\nSurveillance complete: 18 days.\nAuthorize simultaneous termination.\nCoordinate with dist9 — 0400.\n\n[SIG: 0xBE0441 // SILENT]',
        'FROM: Data Monetization Group\nTO: Board of Directors\nRE: Neural Signature Auction — Q3\n\n88,000 unique profiles ready.\nSale to NeuralState: Φ8.8M.\nAnonymization: cosmetic only.\nAudit-proof — legal reviewed.\n\n>>> RELAY: corp/mirror-d7 <<<',
        'PRIORITY ALPHA\nFROM: Enforcement Central\nTO: ALL DISTRICT COMMANDS\nRE: Operation Dark Census\n\nRound up all unregistered operators.\nBCI registration mandatory by 0600.\nNon-compliance: level 3 detention.\nMedia blackout in effect.\n\n[RELAY CORRUPTION — CRITICAL]',
    ];

    // ── Memo erasure — JS character-by-character erase ──────────────────────
    function eraseMemo(el) {
        var BLOCK = '█▓▒░▪■▫';
        var chars = el.textContent.split('');
        var total = chars.length;
        var phase = 0; // 0 = scramble with blocks, 1 = clear to spaces
        var count = 0;
        var timer = setInterval(function () {
            var n = rand(4, 10);
            for (var i = 0; i < n; i++) {
                var idx = rand(0, total - 1);
                if (chars[idx] === '\n') continue;
                if (phase === 0) {
                    chars[idx] = BLOCK[rand(0, BLOCK.length - 1)];
                } else {
                    chars[idx] = ' ';
                }
            }
            el.textContent = chars.join('');
            count += n;
            if (phase === 0 && count >= total * 1.6) { phase = 1; count = 0; }
            if (phase === 1 && count >= total * 1.6) {
                clearInterval(timer);
                if (el.parentNode) el.parentNode.removeChild(el);
            }
        }, 22);
    }

    function spawnMemo() {
        var host = getHost();
        if (!host) return;
        var el = document.createElement('div');
        el.className = 'cbg-memo';
        var mp = bestPos(host, 240, 110, -5, 78, 4, 88);
        el.style.left = mp[0] + '%';
        el.style.top  = mp[1] + '%';
        el.textContent = pick(MEMOS);
        host.appendChild(el);
        setTimeout(function () { eraseMemo(el); }, rand(3000, 6000));
    }


    // ── Scrolling texture layer ─────────────────────────────────────────────

    var TEX_SRCS = [
        '/api/media/circuitboard.00.png',
        '/api/media/circuitboard.01.png',
        '/api/media/circuitboard.02.png',
    ];
    var texLayers = null;   // built once; images reused across navigations
    var texRaf    = null;
    var texTimer  = null;

    function initTextures() {
        var host = getHost();
        if (!host) return;

        // Reuse or create canvas inside host
        var canvas = host.querySelector('.cbg-tex');
        if (!canvas) {
            canvas = document.createElement('canvas');
            canvas.className = 'cbg-tex';
            canvas.style.cssText = 'position:absolute;inset:0;pointer-events:none;z-index:0;';
            host.insertBefore(canvas, host.firstChild);
        }
        canvas.width  = window.innerWidth;
        canvas.height = window.innerHeight;
        var ctx = canvas.getContext('2d');

        // Build layer descriptors once; images survive page navigations
        if (!texLayers) {
            texLayers = TEX_SRCS.map(function (src, i) {
                var img = new Image();
                img.src = src;
                var angle = (Math.PI * 2 / 3) * i + 0.4;
                var speed = 0.10 + i * 0.03;
                return {
                    img:     img,
                    dx:      i * 80,
                    dy:      i * 55,
                    vx:      Math.cos(angle) * speed,
                    vy:      Math.sin(angle) * speed,
                    opacity: [0.015, 0.010, 0.008][i],
                };
            });
        }

        // Direction randomiser — shift one layer's heading every 3-7s
        function schedDir() {
            texTimer = setTimeout(function () {
                if (!texLayers) return;
                var t = texLayers[Math.floor(Math.random() * texLayers.length)];
                var a = Math.random() * Math.PI * 2;
                var s = 0.07 + Math.random() * 0.13;
                t.vx = Math.cos(a) * s;
                t.vy = Math.sin(a) * s;
                schedDir();
            }, 3000 + Math.random() * 4000);
        }
        if (!texTimer) schedDir();

        // Restart animation loop on the (possibly new) canvas
        if (texRaf) cancelAnimationFrame(texRaf);
        var w = canvas.width, h = canvas.height;

        function frame() {
            ctx.clearRect(0, 0, w, h);
            texLayers.forEach(function (t) {
                if (!t.img.complete || !t.img.naturalWidth) return;
                var iw = t.img.width, ih = t.img.height;
                t.dx = ((t.dx + t.vx) % iw + iw) % iw;
                t.dy = ((t.dy + t.vy) % ih + ih) % ih;
                ctx.globalAlpha = t.opacity;
                for (var x = -t.dx; x < w + iw; x += iw) {
                    for (var y = -t.dy; y < h + ih; y += ih) {
                        ctx.drawImage(t.img, x, y);
                    }
                }
            });
            ctx.globalAlpha = 1;
            texRaf = requestAnimationFrame(frame);
        }
        frame();
    }

    // ── Tick loop ───────────────────────────────────────────────────────────

    // ── Floating artifact clusters ───────────────────────────────────────────

    function spawnArtifact() {
        var host = getHost();
        if (!host) return;

        var palettes = ['cbg-artifact--red', 'cbg-artifact--white', 'cbg-artifact--blue', 'cbg-artifact--amber'];
        var el = document.createElement('div');
        el.className = 'cbg-artifact ' + pick(palettes);
        el.style.left = rand(-2, 90) + '%';
        el.style.top  = rand(-2, 90) + '%';
        el.style.opacity = '0';
        el.style.transition = 'opacity 0.55s ease';

        var n = rand(18, 42);
        var blobR = rand(45, 90);
        for (var i = 0; i < n; i++) {
            var span = document.createElement('span');
            span.className = 'cbg-artifact-char';
            var angle  = Math.random() * Math.PI * 2;
            var r      = Math.sqrt(Math.random()) * blobR;
            span.style.left       = Math.round(r * Math.cos(angle)) + 'px';
            span.style.top        = Math.round(r * Math.sin(angle)) + 'px';
            span.style.fontSize   = rand(9, 20) + 'px';
            span.style.filter     = 'blur(' + (0.8 + Math.random() * 3.2).toFixed(1) + 'px)';
            span.style.animationDelay = (Math.random() * 0.85).toFixed(2) + 's';
            span.textContent = GLYPH_CHARS[Math.floor(Math.random() * GLYPH_CHARS.length)];
            el.appendChild(span);
        }

        host.appendChild(el);

        // Fade in after paint
        requestAnimationFrame(function () {
            requestAnimationFrame(function () { el.style.opacity = '1'; });
        });

        // After idle, either drift-then-vanish or just dissolve
        var idleMs = rand(600, 2200);
        setTimeout(function () {
            if (!el.parentNode) return;
            if (Math.random() < 0.65) {
                // Drift: transform starts, opacity fades out faster — it vanishes before arriving
                var driftAngle = Math.random() * Math.PI * 2;
                var dist = rand(22, 65);
                var dx = Math.round(Math.cos(driftAngle) * dist);
                var dy = Math.round(Math.sin(driftAngle) * dist);
                el.style.transition = 'transform 3s ease-out, opacity 1.6s ease-out';
                el.style.transform  = 'translate(' + dx + 'px, ' + dy + 'px)';
                el.style.opacity    = '0';
            } else {
                el.style.transition = 'opacity 1.2s ease';
                el.style.opacity    = '0';
            }
            setTimeout(function () {
                if (el.parentNode) el.parentNode.removeChild(el);
            }, 3200);
        }, idleMs);
    }

    // ── Spawn rate constants — edit here to tune both hosts identically ────────
    var RATE_ERROR    = 0.02;  // fatal error popups
    var RATE_WARN     = 0.02;  // warning popups
    var RATE_MEMO     = 0.04;  // corporate memo intercepts
    var RATE_GEO      = 0.10;  // geometric schematic windows
    var RATE_CASCADE  = 0.03;  // cascading console window burst
    var RATE_ARTIFACT = 0.05;  // floating glyph artifact clusters
    var RATE_FRAG     = 0.51;  // floating code fragments
    // RATE_WIN = remainder (~0.23) — console windows (black / blue / amber)

    function tickDelay() {
        var area = window.innerWidth * window.innerHeight;
        var scale = Math.max(0.35, Math.min(2.5, (1920 * 1080) / area));
        return rand(500, 1800) * scale;
    }

    function tick() {
        if (!getHost()) { tickTimer = null; return; }
        var r = Math.random(), t = 0;
        if      (r < (t += RATE_ERROR))    spawnError();
        else if (r < (t += RATE_WARN))     spawnWarning();
        else if (r < (t += RATE_MEMO))     spawnMemo();
        else if (r < (t += RATE_GEO))      spawnGeoWindow();
        else if (r < (t += RATE_CASCADE))  spawnCascade();
        else if (r < (t += RATE_ARTIFACT)) spawnArtifact();
        else if (r < (t += RATE_FRAG))     spawnFrag();
        else                                spawnWindow();
        tickTimer = setTimeout(tick, tickDelay());
    }

    function start() {
        var host = getHost();
        if (!host) return;
        initTextures();           // always reinit textures on this host
        if (tickTimer) return;    // tick loop already running
        tickTimer = setTimeout(tick, rand(500, 1500));
    }

    // Initial start
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', start);
    } else {
        start();
    }

    // Restart whenever .console-bg-host is added back to the DOM (Blazor navigation)
    new MutationObserver(function (mutations) {
        for (var i = 0; i < mutations.length; i++) {
            var nodes = mutations[i].addedNodes;
            for (var j = 0; j < nodes.length; j++) {
                var n = nodes[j];
                if (n.nodeType !== 1) continue;
                if ((n.classList && n.classList.contains('console-bg-host')) ||
                    (n.querySelector && n.querySelector('.console-bg-host'))) {
                    start();
                    return;
                }
            }
        }
    }).observe(document.body, { childList: true, subtree: true });

    return { start: start };
})();
