package main

import (
	"os"
	"path"

	"k8s.io/client-go/kubernetes"
	v1 "k8s.io/client-go/kubernetes/typed/core/v1"
	"k8s.io/client-go/rest"
	"k8s.io/client-go/tools/clientcmd"
	// core
	// "k8s.io/apimachinery/pkg/api/errors"
	// metav1 "k8s.io/apimachinery/pkg/apis/meta/v1"
	// "k8s.io/client-go/kubernetes"
	// "k8s.io/client-go/rest"
	//
	// Uncomment to load all auth plugins
	// _ "k8s.io/client-go/plugin/pkg/client/auth"
	//
	// Or uncomment to load specific auth plugins
	// _ "k8s.io/client-go/plugin/pkg/client/auth/azure"
	// _ "k8s.io/client-go/plugin/pkg/client/auth/gcp"
	// _ "k8s.io/client-go/plugin/pkg/client/auth/oidc"
	// _ "k8s.io/client-go/plugin/pkg/client/auth/openstack"
)

func getConfig() (*rest.Config, error) {
	var config *rest.Config
	var err error

	inPod := os.Getenv("KUBERNETES_SERVICE_HOST") != ""
	if inPod {
		config, err = rest.InClusterConfig()
	} else {

		// uses the current context in kubeconfig
		// path-to-kubeconfig -- for example, /root/.kube/config
		homeDir, err := os.UserHomeDir()
		if err != nil {
			return nil, err
		}

		configPath := path.Join(homeDir, ".kube", "config")
		config, err = clientcmd.BuildConfigFromFlags("", configPath)
	}

	return config, err
}

func getCoreV1() (v1.CoreV1Interface, error) {
	config, err := getConfig()
	if err != nil {
		return nil, err
	}

	// creates the clientset
	clientset, err := kubernetes.NewForConfig(config)
	if err != nil {
		return nil, err
	}

	// access the API to list pods
	coreV1 := clientset.CoreV1()
	return coreV1, nil
	//pods, _ := clientset.CoreV1().Pods("").List(v1.ListOptions{})
}
